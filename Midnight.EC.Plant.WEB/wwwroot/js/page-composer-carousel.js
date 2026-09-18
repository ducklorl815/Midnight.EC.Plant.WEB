(function () {
  "use strict";

  function initCarousel(root) {
    var slides = Array.prototype.slice.call(root.querySelectorAll(".pc-detail-wall-slide"));
    var dots = Array.prototype.slice.call(root.querySelectorAll(".pc-detail-wall-dot"));

    // Legacy fallback: old media/copy split markup
    if (slides.length === 0) {
      var medias = Array.prototype.slice.call(root.querySelectorAll(".pc-detail-wall-slide-media"));
      var copies = Array.prototype.slice.call(root.querySelectorAll(".pc-detail-wall-slide-copy"));
      var countLegacy = Math.max(medias.length, copies.length);
      if (countLegacy <= 1) return;
      var indexL = 0;
      var intervalLegacy = parseInt(root.getAttribute("data-interval") || "8500", 10);
      if (!Number.isFinite(intervalLegacy) || intervalLegacy < 2000) intervalLegacy = 8500;
      var timerL = null;
      function showL(i) {
        indexL = ((i % countLegacy) + countLegacy) % countLegacy;
        medias.forEach(function (el, n) {
          el.classList.toggle("is-active", n === indexL);
        });
        copies.forEach(function (el, n) {
          el.classList.toggle("is-active", n === indexL);
        });
      dots.forEach(function (el) {
        var on = parseInt(el.getAttribute("data-slide-to") || "-1", 10) === indexL;
        el.classList.toggle("is-active", on);
        el.setAttribute("aria-selected", on ? "true" : "false");
      });
      }
      function startL() {
        stopL();
        timerL = window.setInterval(function () {
          showL(indexL + 1);
        }, intervalLegacy);
      }
      function stopL() {
        if (timerL) {
          window.clearInterval(timerL);
          timerL = null;
        }
      }
      dots.forEach(function (dot) {
        dot.addEventListener("click", function () {
          showL(parseInt(dot.getAttribute("data-slide-to") || "0", 10));
          startL();
        });
      });
      root.addEventListener("mouseenter", stopL);
      root.addEventListener("mouseleave", startL);
      showL(0);
      startL();
      return;
    }

    var count = slides.length;
    if (count <= 1) return;

    var index = 0;
    var intervalMs = parseInt(root.getAttribute("data-interval") || "8500", 10);
    if (!Number.isFinite(intervalMs) || intervalMs < 2000) intervalMs = 8500;
    var timer = null;

    function show(i) {
      index = ((i % count) + count) % count;
      slides.forEach(function (el, n) {
        el.classList.toggle("is-active", n === index);
      });
      dots.forEach(function (el) {
        var on = parseInt(el.getAttribute("data-slide-to") || "-1", 10) === index;
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
        show(parseInt(dot.getAttribute("data-slide-to") || "0", 10));
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
