// File download helper
window.downloadFile = function (fileName, contentType, base64Data) {
    const linkElement = document.createElement('a');
    linkElement.setAttribute('href', `data:${contentType};base64,${base64Data}`);
    linkElement.setAttribute('download', fileName);
    linkElement.style.display = 'none';
    document.body.appendChild(linkElement);
    linkElement.click();
    document.body.removeChild(linkElement);
};
