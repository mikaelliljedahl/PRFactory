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

def handle_connect(client_socket, target_host, target_port):
    """Handle HTTPS CONNECT tunneling"""
    try:
        # Send success response to client
        client_socket.sendall(b'HTTP/1.1 200 Connection Established\r\n\r\n')

        # Use curl to establish connection through the outer proxy
        # We'll use curl's --proxy-tunnel to tunnel HTTPS through the proxy

        # For HTTPS, we need to tunnel the raw TCP connection
        # Since curl can handle the outer proxy, we'll create a subprocess
        # that forwards data between client and curl

        # Actually, let's use a simpler approach: use Python's socket to connect
        # through the proxy using CONNECT method

        outer_proxy = ('21.0.0.113', 15004)
        proxy_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        proxy_socket.connect(outer_proxy)

        # Send CONNECT to outer proxy
        connect_request = f'CONNECT {target_host}:{target_port} HTTP/1.1\r\n'
        connect_request += f'Host: {target_host}:{target_port}\r\n'

        # Add proxy authentication from environment
        import os
        proxy_url = os.environ.get('HTTPS_PROXY', '')
        if '@' in proxy_url:
            # Extract credentials
            import base64
            auth_part = proxy_url.split('//')[1].split('@')[0]
            user, password = auth_part.split(':', 1)
            credentials = base64.b64encode(f'{user}:{password}'.encode()).decode()
            connect_request += f'Proxy-Authorization: Basic {credentials}\r\n'

        connect_request += '\r\n'

        proxy_socket.sendall(connect_request.encode())

        # Read response from outer proxy
        response = b''
        while b'\r\n\r\n' not in response:
            chunk = proxy_socket.recv(1024)
            if not chunk:
                break
            response += chunk

        # Check if connection succeeded
        if b'200' not in response.split(b'\r\n')[0]:
            print(f"Proxy CONNECT failed: {response[:200]}", file=sys.stderr)
            client_socket.close()
            proxy_socket.close()
            return

        # Now tunnel data between client and proxy_socket
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
                except:
                    return

    except Exception as e:
        print(f"CONNECT tunnel error: {e}", file=sys.stderr)
    finally:
        try:
            client_socket.close()
        except:
            pass
        try:
            proxy_socket.close()
        except:
            pass

def handle_http_request(client_socket, method, url, headers, body=None):
    """Handle regular HTTP request using curl"""
    try:
        # Build curl command
        cmd = ['curl', '-s', '-i', '-X', method]

        # Add headers
        for key, value in headers.items():
            if key.lower() not in ['host', 'connection', 'proxy-connection']:
                cmd.extend(['-H', f'{key}: {value}'])

        # Add body for POST/PUT
        if body:
            cmd.extend(['--data-binary', '@-'])

        # Add URL
        cmd.append(url)

        # Execute curl (it will use proxy from environment)
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

def handle_client(client_socket):
    """Handle a client connection"""
    try:
        # Read the request
        request_data = b''
        while b'\r\n\r\n' not in request_data:
            chunk = client_socket.recv(4096)
            if not chunk:
                return
            request_data += chunk

        # Parse request
        request_lines = request_data.split(b'\r\n')
        request_line = request_lines[0].decode('utf-8', errors='ignore')

        parts = request_line.split(' ')
        if len(parts) < 3:
            client_socket.close()
            return

        method, path, protocol = parts[0], parts[1], parts[2]

        # Parse headers
        headers = {}
        for line in request_lines[1:]:
            if not line:
                break
            if b':' in line:
                key, value = line.decode('utf-8', errors='ignore').split(':', 1)
                headers[key.strip()] = value.strip()

        # Handle CONNECT for HTTPS
        if method == 'CONNECT':
            host, port = path.split(':')
            handle_connect(client_socket, host, int(port))
        else:
            # Regular HTTP request
            # Extract body if present
            body = None
            if b'\r\n\r\n' in request_data:
                body = request_data.split(b'\r\n\r\n', 1)[1]

            handle_http_request(client_socket, method, path, headers, body)

    except Exception as e:
        print(f"Client handler error: {e}", file=sys.stderr)
        try:
            client_socket.close()
        except:
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
