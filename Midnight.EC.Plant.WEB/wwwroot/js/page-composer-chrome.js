/**
 * 滾動態殼層：一進頁頂欄即貼頂跟著滾；過 Banner 底緣後改白底深色字。
 * 首頁用 fixed；拼圖預覽用 absolute + scrollTop 同步（容器內捲）。
 */
(function () {
  function bindShell(shell) {
    if (!shell || shell.dataset.pcChromeBound === "1") return;
    var header = shell.querySelector(".pc-site-header");
    var banner = shell.querySelector(".pc-module.pc-banner");
    if (!header || !banner) return;

    shell.dataset.pcChromeBound = "1";
    var isPreview = shell.classList.contains("is-preview");

    function update() {
      var pastBanner;
      if (isPreview) {
        var y = shell.scrollTop;
        header.style.top = y + "px";
        pastBanner = y >= banner.offsetTop + banner.offsetHeight;
      } else {
        header.style.top = "";
        pastBanner = banner.getBoundingClientRect().bottom <= 0;
      }
      header.classList.toggle("is-sticky", pastBanner);
    }

    if (isPreview) {
      shell.addEventListener("scroll", update, { passive: true });
    } else {
      window.addEventListener("scroll", update, { passive: true });
      window.addEventListener("resize", update, { passive: true });
    }
    update();
  }

  function bindAll(root) {
    var scope = root || document;
    if (scope.classList && scope.classList.contains("pc-shell")) {
      bindShell(scope);
      return;
    }
    scope.querySelectorAll(".pc-shell").forEach(bindShell);
  }

  window.pcBindChrome = bindAll;

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", function () {
      bindAll(document);
    });
  } else {
    bindAll(document);
  }
})();
