(function () {
  "use strict";

  function initCarousel(root) {
    var medias = Array.prototype.slice.call(root.querySelectorAll(".pc-detail-wall-slide-media"));
    var copies = Array.prototype.slice.call(root.querySelectorAll(".pc-detail-wall-slide-copy"));
    var dots = Array.prototype.slice.call(root.querySelectorAll(".pc-detail-wall-dot"));
    var count = Math.max(medias.length, copies.length);
    if (count <= 1) return;

    var index = 0;
    var intervalMs = parseInt(root.getAttribute("data-interval") || "5500", 10);
    if (!Number.isFinite(intervalMs) || intervalMs < 2000) intervalMs = 5500;
    var timer = null;

    function show(i) {
      index = ((i % count) + count) % count;
      medias.forEach(function (el, n) {
        el.classList.toggle("is-active", n === index);
      });
      copies.forEach(function (el, n) {
        el.classList.toggle("is-active", n === index);
      });
      dots.forEach(function (el, n) {
        var on = n === index;
        el.classList.toggle("is-active", on);
        el.setAttribute("aria-selected", on ? "true" : "false");
      });
    }

    function next() {
      show(index + 1);
    }

    function start() {
      stop();
      timer = window.setInterval(next, intervalMs);
    }

    function stop() {
      if (timer) {
        window.clearInterval(timer);
        timer = null;
      }
    }

    dots.forEach(function (dot) {
      dot.addEventListener("click", function () {
        var to = parseInt(dot.getAttribute("data-slide-to") || "0", 10);
        show(to);
        start();
      });
    });

    root.addEventListener("mouseenter", stop);
    root.addEventListener("mouseleave", start);
    root.addEventListener("focusin", stop);
    root.addEventListener("focusout", start);

    show(0);
    start();
  }

  document.querySelectorAll("[data-pc-carousel]").forEach(initCarousel);
})();
