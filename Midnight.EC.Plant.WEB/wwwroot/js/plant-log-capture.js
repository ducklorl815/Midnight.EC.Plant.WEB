(function () {
  const input = document.getElementById("log-photos");
  const cameraBtn = document.getElementById("log-camera");
  const galleryBtn = document.getElementById("log-gallery");
  if (!input || !cameraBtn || !galleryBtn) return;

  const cap = window.Capacitor;
  const camera = cap && cap.Plugins && cap.Plugins.Camera;
  const native = cap && typeof cap.isNativePlatform === "function" && cap.isNativePlatform();
  if (!native || !camera || typeof camera.getPhoto !== "function") return;

  cameraBtn.classList.remove("d-none");
  galleryBtn.classList.remove("d-none");

  async function addPhoto(source) {
    const photo = await camera.getPhoto({
      quality: 85,
      allowEditing: false,
      resultType: "base64",
      source: source
    });
    if (!photo || !photo.base64String) return;
    const bytes = Uint8Array.from(atob(photo.base64String), function (c) { return c.charCodeAt(0); });
    const type = photo.format === "png" ? "image/png" : "image/jpeg";
    const ext = photo.format === "png" ? "png" : "jpg";
    const file = new File([bytes], "plant-" + Date.now() + "." + ext, { type: type });
    const transfer = new DataTransfer();
    Array.from(input.files || []).forEach(function (existing) { transfer.items.add(existing); });
    transfer.items.add(file);
    input.files = transfer.files;
  }

  cameraBtn.addEventListener("click", function () {
    addPhoto("CAMERA").catch(function (err) { alert(err && err.message ? err.message : "無法開啟相機"); });
  });
  galleryBtn.addEventListener("click", function () {
    addPhoto("PHOTOS").catch(function (err) { alert(err && err.message ? err.message : "無法開啟相簿"); });
  });
})();
