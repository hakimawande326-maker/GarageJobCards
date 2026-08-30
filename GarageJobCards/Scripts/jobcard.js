(function () {
    var dragged = null;

    document.querySelectorAll(".job-card-mini").forEach(function (card) {
        card.addEventListener("dragstart", function (e) {
            dragged = card;
            card.classList.add("dragging");
            e.dataTransfer.effectAllowed = "move";
        });
        card.addEventListener("dragend", function () {
            card.classList.remove("dragging");
            dragged = null;
        });
    });

    document.querySelectorAll(".board-column-body").forEach(function (column) {
        column.addEventListener("dragover", function (e) {
            e.preventDefault();
            column.classList.add("drag-over");
        });
        column.addEventListener("dragleave", function () {
            column.classList.remove("drag-over");
        });
        column.addEventListener("drop", function (e) {
            e.preventDefault();
            column.classList.remove("drag-over");
            if (!dragged) return;

            var jobId = dragged.getAttribute("data-id");
            var newStatus = column.getAttribute("data-status");
            var previousParent = dragged.parentElement;

            column.appendChild(dragged);

            fetch("/JobCard/UpdateStatus", {
                method: "POST",
                headers: { "Content-Type": "application/x-www-form-urlencoded" },
                body: "id=" + encodeURIComponent(jobId) + "&status=" + encodeURIComponent(newStatus)
            })
                .then(function (r) { return r.json(); })
                .then(function (result) {
                    if (!result.success) {
                        previousParent.appendChild(dragged);
                        alert(result.message || "Could not update this job card.");
                    } else {
                        updateColumnCounts();
                    }
                })
                .catch(function () {
                    previousParent.appendChild(dragged);
                    alert("Network error - the move wasn't saved.");
                });
        });
    });

    function updateColumnCounts() {
        document.querySelectorAll(".board-column").forEach(function (col) {
            var cards = col.querySelectorAll(".job-card-mini");
            var visibleCount = 0;
            cards.forEach(function (c) {
                if (c.style.display !== "none") visibleCount++;
            });
            var badge = col.querySelector(".count");
            if (badge) badge.textContent = visibleCount;
        });
    }
})();

// ---------- Global password visibility toggle ----------
// Works on any input wrapped in .password-wrap with a .password-toggle
// button that has data-target="<input id>". Wired up once here so every
// page automatically gets it without repeating JS per view.
(function () {
    function attachToggles() {
        document.querySelectorAll(".password-toggle").forEach(function (btn) {
            if (btn.dataset.toggleAttached) return;
            btn.dataset.toggleAttached = "1";

            btn.addEventListener("click", function () {
                var input = document.getElementById(btn.getAttribute("data-target"));
                if (!input) return;
                if (input.type === "password") {
                    input.type = "text";
                    btn.textContent = "Hide";
                } else {
                    input.type = "password";
                    btn.textContent = "Show";
                }
            });
        });
    }
    attachToggles();
    document.addEventListener("DOMContentLoaded", attachToggles);
})();

// ---------- Global 3D tilt effect (job cards + form cards) ----------
// Lightweight, CSS-only-transform based - tracks the mouse and tilts the
// element toward the cursor for a subtle "physical" depth feel. Skipped
// entirely if the user prefers reduced motion.
(function () {
    var prefersReducedMotion = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    if (prefersReducedMotion) return;

    var MAX_TILT = 6; // degrees

    function attachTilt(el) {
        if (el.dataset.tiltAttached) return;
        el.dataset.tiltAttached = "1";

        el.addEventListener("mousemove", function (e) {
            var rect = el.getBoundingClientRect();
            var x = (e.clientX - rect.left) / rect.width;  // 0 to 1
            var y = (e.clientY - rect.top) / rect.height;  // 0 to 1
            var rotateY = (x - 0.5) * MAX_TILT * 2;
            var rotateX = (0.5 - y) * MAX_TILT * 2;
            el.classList.add("tilting");
            el.style.transform = "perspective(700px) rotateX(" + rotateX.toFixed(2) + "deg) rotateY(" + rotateY.toFixed(2) + "deg) translateZ(0)";
        });

        el.addEventListener("mouseleave", function () {
            el.classList.remove("tilting");
            el.style.transform = "";
        });
    }

    function attachAll() {
        document.querySelectorAll(".job-card-mini, .form-card").forEach(attachTilt);
    }

    // Run now, and again shortly after in case content loads/renders late
    // (e.g. inside a section that streams in after DOMContentLoaded).
    attachAll();
    document.addEventListener("DOMContentLoaded", attachAll);
    setTimeout(attachAll, 500);
})();