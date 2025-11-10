#!/usr/bin/env python3
"""
NuGet Proxy - HTTPS tunneling proxy that uses curl for outbound requests.
This works around .NET HttpClient's proxy authentication issues.
"""

import socket
import threading
import subprocess
import sys
import select
from urllib.parse import urlparse

PROXY_PORT = 8888
BUFFER_SIZE = 8192

def _parse_proxy_url():
    """Extract proxy host and port from environment"""
    import os
    import re
    proxy_url = os.environ.get('HTTPS_PROXY', '')

    # Claude Code proxy format: http://container_name:jwt_TOKEN@HOST:PORT
    # Extract host:port from the end
    match = re.search(r'@([^@:]+):(\d+)', proxy_url)
    if match:
        return match.group(1), int(match.group(2))

    # Fallback to default
    return '21.0.0.115', 15004

def _get_proxy_auth(proxy_url):
    """Extract and encode proxy authentication"""
    if '@' not in proxy_url:
        return None

    import base64
    auth_part = proxy_url.split('//')[1].split('@')[0]
    credentials = base64.b64encode(auth_part.encode()).decode()
    return credentials

def _send_connect_request(proxy_socket, target_host, target_port, proxy_url):
    """Send CONNECT request to outer proxy"""
    connect_request = f'CONNECT {target_host}:{target_port} HTTP/1.1\r\n'
    connect_request += f'Host: {target_host}:{target_port}\r\n'

    # Add proxy authentication from environment
    auth = _get_proxy_auth(proxy_url)
    if auth:
        connect_request += f'Proxy-Authorization: Basic {auth}\r\n'

    connect_request += '\r\n'
    proxy_socket.sendall(connect_request.encode())

def _read_proxy_response(proxy_socket):
    """Read response from outer proxy"""
    response = b''
    while b'\r\n\r\n' not in response:
        chunk = proxy_socket.recv(1024)
        if not chunk:
            break
        response += chunk
    return response

def _tunnel_data(client_socket, proxy_socket):
    """Tunnel data between client and proxy socket"""
    sockets = [client_socket, proxy_socket]

    while True:
        readable, _, exceptional = select.select(sockets, [], sockets, 60)

        if exceptional:
            break

        for sock in readable:
            try:
                data = sock.recv(BUFFER_SIZE)
                if not data:
                    return

                # Forward to the other socket
                if sock is client_socket:
                    proxy_socket.sendall(data)
                else:
                    client_socket.sendall(data)
            except Exception as e:
                return

def handle_connect(client_socket, target_host, target_port):
    """Handle HTTPS CONNECT tunneling"""
    import os
    proxy_socket = None

    try:
        # Send success response to client
        client_socket.sendall(b'HTTP/1.1 200 Connection Established\r\n\r\n')

        # Parse outer proxy from environment
        proxy_url = os.environ.get('HTTPS_PROXY', '')
        proxy_host, proxy_port = _parse_proxy_url()

        # Connect to outer proxy
        outer_proxy = (proxy_host, proxy_port)
        proxy_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        proxy_socket.settimeout(30)
        proxy_socket.connect(outer_proxy)

        # Send CONNECT request to outer proxy
        _send_connect_request(proxy_socket, target_host, target_port, proxy_url)

        # Read response from outer proxy
        response = _read_proxy_response(proxy_socket)

        # Check if connection succeeded
        if b'200' not in response.split(b'\r\n')[0]:
            print(f"Proxy CONNECT failed: {response[:200]}", file=sys.stderr)
            return

        # Tunnel data between client and proxy socket
        _tunnel_data(client_socket, proxy_socket)

    except Exception as e:
        print(f"CONNECT tunnel error: {e}", file=sys.stderr)
    finally:
        try:
            client_socket.close()
        except Exception as e:
            pass
        try:
            if proxy_socket:
                proxy_socket.close()
        except Exception as e:
            pass

def _build_curl_command(method, url, headers, body=None):
    """Build curl command with headers and body"""
    cmd = ['curl', '-s', '-i', '-X', method]

    # Add headers (skip proxy-specific ones)
    for key, value in headers.items():
        if key.lower() not in ['host', 'connection', 'proxy-connection']:
            cmd.extend(['-H', f'{key}: {value}'])

    # Add body for POST/PUT
    if body:
        cmd.extend(['--data-binary', '@-'])

    # Add URL
    cmd.append(url)
    return cmd

def handle_http_request(client_socket, method, url, headers, body=None):
    """Handle regular HTTP request using curl"""
    try:
        # Build and execute curl command
        cmd = _build_curl_command(method, url, headers, body)
        proc = subprocess.Popen(
            cmd,
            stdin=subprocess.PIPE if body else None,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE
        )

        stdout, stderr = proc.communicate(input=body if body else None, timeout=30)

        # Send response back to client
        client_socket.sendall(stdout)

    except Exception as e:
        print(f"HTTP request error: {e}", file=sys.stderr)
        error_response = b'HTTP/1.1 500 Internal Server Error\r\n\r\nProxy error'
        client_socket.sendall(error_response)
    finally:
        client_socket.close()

def _read_request_data(client_socket):
    """Read complete HTTP request from client"""
    request_data = b''
    while b'\r\n\r\n' not in request_data:
        chunk = client_socket.recv(4096)
        if not chunk:
            return None
        request_data += chunk
    return request_data

def _parse_request_line(request_data):
    """Parse HTTP request line into method, path, protocol"""
    request_lines = request_data.split(b'\r\n')
    request_line = request_lines[0].decode('utf-8', errors='ignore')

    parts = request_line.split(' ')
    if len(parts) < 3:
        return None

    return parts[0], parts[1], parts[2]

def _parse_headers(request_data):
    """Parse HTTP headers from request data"""
    request_lines = request_data.split(b'\r\n')
    headers = {}

    for line in request_lines[1:]:
        if not line:
            break
        if b':' in line:
            key, value = line.decode('utf-8', errors='ignore').split(':', 1)
            headers[key.strip()] = value.strip()

    return headers

def _extract_body(request_data):
    """Extract request body if present"""
    if b'\r\n\r\n' in request_data:
        return request_data.split(b'\r\n\r\n', 1)[1]
    return None

def handle_client(client_socket):
    """Handle a client connection"""
    try:
        # Read the request
        request_data = _read_request_data(client_socket)
        if request_data is None:
            return

        # Parse request line
        request_info = _parse_request_line(request_data)
        if request_info is None:
            client_socket.close()
            return

        method, path, protocol = request_info

        # Parse headers
        headers = _parse_headers(request_data)

        # Route request based on method
        if method == 'CONNECT':
            host, port = path.split(':')
            handle_connect(client_socket, host, int(port))
        else:
            # Regular HTTP request
            body = _extract_body(request_data)
            handle_http_request(client_socket, method, path, headers, body)

    except Exception as e:
        print(f"Client handler error: {e}", file=sys.stderr)
        try:
            client_socket.close()
        except Exception as e:
            pass

def start_proxy():
    """Start the proxy server"""
    server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    server.bind(('127.0.0.1', PROXY_PORT))
    server.listen(50)

    print(f"🔧 NuGet Proxy started on http://127.0.0.1:{PROXY_PORT}")
    print(f"   Configure .NET with:")
    print(f"   export HTTP_PROXY=http://127.0.0.1:{PROXY_PORT}")
    print(f"   export HTTPS_PROXY=http://127.0.0.1:{PROXY_PORT}")
    print()

    try:
        while True:
            client_socket, addr = server.accept()
            # Handle each client in a thread
            thread = threading.Thread(target=handle_client, args=(client_socket,))
            thread.daemon = True
            thread.start()
    except KeyboardInterrupt:
        print("\n✅ Proxy stopped")
    finally:
        server.close()

if __name__ == '__main__':
    start_proxy()
