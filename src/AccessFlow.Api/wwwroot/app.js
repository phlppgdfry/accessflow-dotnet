const api = "/api";
const dialog = document.querySelector("#submit-dialog");
const queue = document.querySelector("#review-queue");
const template = document.querySelector("#item-template");

const statusText = { NeedsReview: "Needs review", Approved: "Approved", ChangesRequested: "Changes requested" };
const severityClass = { Critical: "critical", High: "high", Medium: "medium", Low: "low" };

async function request(path, options) {
  const response = await fetch(`${api}${path}`, { headers: { "Content-Type": "application/json" }, ...options });
  if (!response.ok) {
    const payload = await response.json().catch(() => ({}));
    throw new Error(payload.detail || payload.title || "Something went wrong.");
  }
  return response.status === 204 ? null : response.json();
}

function setMetrics(summary) {
  document.querySelector("#metric-total").textContent = summary.totalItems;
  document.querySelector("#metric-review").textContent = summary.needsReview;
  document.querySelector("#metric-approved").textContent = summary.approved;
  document.querySelector("#metric-findings").textContent = summary.highPriorityFindings;
  document.querySelector("#queue-count").textContent = summary.needsReview;
}

function makeFinding(finding) {
  const node = document.createElement("div");
  node.className = `finding ${severityClass[finding.severity] || "medium"}`;
  // Text only: findings quote HTML such as <html lang="nl"> and must never be parsed as markup.
  const heading = document.createElement("strong");
  heading.textContent = `${finding.ruleCode} · ${finding.title}`;
  const advice = document.createElement("span");
  advice.textContent = finding.recommendation;
  node.append(heading, advice);
  return node;
}

function renderItems(items) {
  queue.replaceChildren();
  items.forEach((item) => {
    const card = template.content.cloneNode(true);
    const root = card.querySelector(".review-card");
    root.dataset.itemId = item.id;
    card.querySelector(".item-source").textContent = new URL(item.sourceUrl).hostname.replace("www.", "");
    card.querySelector(".item-title").textContent = item.title;
    const status = card.querySelector(".status-label");
    status.textContent = statusText[item.status];
    status.classList.add(item.status === "Approved" ? "approved" : item.status === "ChangesRequested" ? "changes-requested" : "needs-review");
    card.querySelector(".finding-summary").textContent = item.findings.length ? `${item.findings.length} checks need context from a reviewer.` : "No automated checks need attention. A human review remains useful.";
    const list = card.querySelector(".finding-list");
    item.findings.forEach((finding) => list.append(makeFinding(finding)));
    const approval = card.querySelector(".approve-button");
    const changes = card.querySelector(".changes-button");
    const suggestion = card.querySelector(".suggestion-button");
    if (item.status !== "NeedsReview") {
      approval.hidden = true;
      changes.hidden = true;
    }
    if (!item.aiConsent) {
      suggestion.disabled = true;
      suggestion.title = "This item has no AI consent.";
    }
    approval.addEventListener("click", () => decide(item.id, "Approved"));
    changes.addEventListener("click", () => decide(item.id, "ChangesRequested"));
    suggestion.addEventListener("click", () => showSuggestion(item.id, root));
    queue.append(card);
  });
}

async function refresh() {
  const [summary, items] = await Promise.all([request("/dashboard"), request("/content-items")]);
  setMetrics(summary);
  renderItems(items);
}

async function decide(id, status) {
  await request(`/content-items/${id}/review`, { method: "POST", body: JSON.stringify({ status, reviewer: "Philippe Godfroy", note: "Reviewed in AccessFlow demo." }) });
  await refresh();
}

async function showSuggestion(id, root) {
  const box = root.querySelector(".suggestion-box");
  try {
    const suggestion = await request(`/content-items/${id}/suggestion`, { method: "POST" });
    const label = document.createElement("strong");
    label.textContent = "Safe suggestion";
    const safeguard = document.createElement("small");
    safeguard.textContent = suggestion.safeguard;
    box.replaceChildren(label, document.createTextNode(suggestion.summary), document.createElement("br"), safeguard);
    box.hidden = false;
  } catch (error) {
    box.textContent = error.message;
    box.hidden = false;
  }
}

document.querySelector("#open-submit").addEventListener("click", () => dialog.showModal());
document.querySelector("#close-submit").addEventListener("click", () => dialog.close());
document.querySelector("#cancel-submit").addEventListener("click", () => dialog.close());
document.querySelector("#submit-form").addEventListener("submit", async (event) => {
  event.preventDefault();
  // Keep a reference: event.currentTarget is null again after the first await.
  const formElement = event.currentTarget;
  const form = new FormData(formElement);
  const error = document.querySelector("#form-error");
  error.textContent = "";
  try {
    await request("/content-items", { method: "POST", body: JSON.stringify({ title: form.get("title"), sourceUrl: form.get("sourceUrl"), html: form.get("html"), submittedBy: form.get("submittedBy"), aiConsent: form.get("aiConsent") === "on" }) });
    dialog.close();
    formElement.reset();
    await refresh();
  } catch (exception) {
    error.textContent = exception.message;
  }
});

refresh().catch((error) => { queue.textContent = `Could not load the review queue: ${error.message}`; });
