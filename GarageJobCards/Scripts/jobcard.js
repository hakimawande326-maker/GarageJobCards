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

 // Optimistically move the card, then confirm with the server.
 column.appendChild(dragged);

 fetch("/JobCard/UpdateStatus", {
 method: "POST",
 headers: { "Content-Type": "application/x-www-form-urlencoded" },
 body: "id=" + encodeURIComponent(jobId) + "&status=" + encodeURIComponent(newStatus)
 })
 .then(function (r) { return r.json(); })
 .then(function (result) {
 if (!result.success) {
 // Roll back if the server rejected the move.
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
