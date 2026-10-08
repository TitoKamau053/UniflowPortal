window.saveFile = (name, base64, type) => {
  const a = document.createElement('a');
  a.href = `data:${type};base64,${base64}`;
  a.download = name;
  document.body.appendChild(a); a.click(); a.remove();
};
