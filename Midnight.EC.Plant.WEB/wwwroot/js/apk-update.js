(function () {
  const cap = window.Capacitor;
  if (!cap || typeof cap.isNativePlatform !== "function" || !cap.isNativePlatform()) return;
  const app = cap.Plugins && cap.Plugins.App;
  const updater = cap.Plugins && cap.Plugins.ApkUpdate;
  if (!app || !updater || typeof app.getInfo !== "function" || typeof updater.install !== "function") return;

  app.getInfo().then(async function (info) {
    const response = await fetch("/app/version.json", { cache: "no-store" });
    if (!response.ok) return;
    const manifest = await response.json();
    const installed = Number(info.build || 0);
    if (!manifest.versionCode || Number(manifest.versionCode) <= installed) return;
    const apkUrl = new URL(manifest.apkUrl || "/app/plant.apk", location.origin).href;
    await updater.install({ url: apkUrl });
  }).catch(function (err) {
    console.warn("apk update skipped", err);
  });
})();
