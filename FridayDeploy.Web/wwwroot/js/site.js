// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
if (window.lucide) {
    window.lucide.createIcons({ attrs: { width: 18, height: 18 } });
}

const sidebarToggle = document.querySelector("[data-sidebar-toggle]");
if (sidebarToggle) {
    sidebarToggle.addEventListener("click", () => {
        const collapsed = document.getElementById("app-shell").classList.toggle("sidebar-collapsed");
        localStorage.setItem("fd-sidebar-collapsed", String(collapsed));
        sidebarToggle.setAttribute("aria-label", collapsed ? "Expand sidebar" : "Collapse sidebar");
        sidebarToggle.setAttribute("title", collapsed ? "Expand sidebar" : "Collapse sidebar");
    });
}

document.addEventListener("keydown", event => {
    if (event.ctrlKey && event.key.toLowerCase() === "k") {
        event.preventDefault();
        document.querySelector("[data-global-search]")?.focus();
    }

    if (event.key === "Escape") {
        document.activeElement?.blur();
    }

    if (event.key === "F5") {
        event.preventDefault();
        window.location.reload();
    }
});

document.querySelectorAll(".copy-button").forEach(button => {
    button.addEventListener("click", async () => {
        const section = button.closest(".copy-section");
        await navigator.clipboard.writeText(section?.innerText.replace("Copy", "").trim() ?? "");
        button.textContent = "Copied";
        setTimeout(() => button.textContent = "Copy", 1200);
    });
});

document.querySelectorAll(".line-numbers").forEach(block => {
    block.innerHTML = block.textContent.split(/\r?\n/).map(line => `<span class="code-line">${escapeHtml(line)}</span>`).join("");
});

document.querySelectorAll(".json-viewer").forEach(block => {
    block.innerHTML = escapeHtml(block.textContent)
        .replace(/("[^"]+"\s*:)/g, '<span class="json-key">$1</span>')
        .replace(/:\s*("[^"]*")/g, ': <span class="json-string">$1</span>')
        .replace(/:\s*(\d+(?:\.\d+)?)/g, ': <span class="json-number">$1</span>');
});

const themeColor = name => getComputedStyle(document.documentElement).getPropertyValue(name).trim();

document.querySelectorAll("canvas[data-chart]").forEach(canvas => {
    const rows = JSON.parse(canvas.dataset.chart || "[]");
    const muted = themeColor("--muted");
    const border = themeColor("--border");
    new Chart(canvas, {
        type: "bar",
        data: { labels: rows.map(x => x.label), datasets: [{ data: rows.map(x => x.count), backgroundColor: themeColor("--info"), borderColor: border }] },
        options: { plugins: { legend: { display: false } }, scales: { x: { ticks: { color: muted }, grid: { color: border } }, y: { ticks: { color: muted }, grid: { color: border } } } }
    });
});

const builder = document.querySelector("#query-builder");
if (builder) {
    const expression = builder.querySelector("[data-expression]");
    const updateExpression = () => {
        const parts = [];
        builder.querySelectorAll("[data-qb]").forEach(input => { if (input.value) parts.push(`${input.dataset.qb}=${input.value}`); });
        builder.querySelectorAll("[data-qb-contains]").forEach(input => { if (input.value) parts.push(`${input.dataset.qbContains}~${input.value}`); });
        const name = builder.querySelector("[data-property-name]")?.value;
        const value = builder.querySelector("[data-property-value]")?.value;
        if (name && value) parts.push(`${name}=${value}`);
        expression.value = parts.join(" AND ");
    };
    builder.addEventListener("input", updateExpression);
    builder.addEventListener("change", updateExpression);
}

function scheduleReloadWithProgress(delayMs) {
    const bar = document.querySelector("[data-loading-bar]");
    if (bar) {
        requestAnimationFrame(() => {
            bar.style.transitionDuration = `${delayMs}ms`;
            bar.style.width = "100%";
        });
    }
    setTimeout(() => window.location.reload(), delayMs);
}

const liveSection = document.querySelector('[data-live="true"]');
if (liveSection) {
    scheduleReloadWithProgress(Number(liveSection.dataset.pollInterval || "5000"));
}

document.querySelectorAll("[data-refresh]").forEach(element => {
    scheduleReloadWithProgress(Number(element.dataset.refresh || "30000"));
});

function escapeHtml(value) {
    return value.replace(/[&<>'"]/g, char => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", "'": "&#39;", '"': "&quot;" })[char]);
}
