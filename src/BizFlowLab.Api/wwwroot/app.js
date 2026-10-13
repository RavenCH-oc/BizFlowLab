"use strict";

// Lesson: Browser -> fetch() -> HTTP API -> JSON -> DOM.
// Important: use textContent for API data, not innerHTML.
const statusFilter = document.querySelector("#status-filter");
const loadOrdersButton = document.querySelector("#load-orders");
const listStatus = document.querySelector("#list-status");
const ordersBody = document.querySelector("#orders-body");
const detailFields = document.querySelector("#detail-fields");
const detailPlaceholder = document.querySelector("#detail-placeholder");
const money = new Intl.NumberFormat("zh-TW", {
  style: "currency", currency: "TWD", minimumFractionDigits: 0, maximumFractionDigits: 2
});

async function getJson(url) {
  const response = await fetch(url, { headers: { Accept: "application/json" }, cache: "no-store" });
  if (!response.ok) {
    // HTTP 400/404/500 don't automatically throw in fetch(). We handle them here.
    const body = await response.json().catch(() => null);
    const title = body?.title ?? body?.message ?? "查詢失敗";
    throw new Error(`HTTP ${response.status}：${title}`);
  }
  return response.json();
}

function setStatus(message, isError = false) {
  listStatus.textContent = message;
  listStatus.classList.toggle("error", isError);
}

function makeCell(value, className = "") {
  const td = document.createElement("td");
  td.textContent = String(value ?? "");
  if (className) td.className = className;
  return td;
}

function renderOrders(orders) {
  ordersBody.replaceChildren();
  if (orders.length === 0) {
    const row = document.createElement("tr");
    const cell = makeCell("沒有符合條件的訂單", "empty");
    cell.colSpan = 6;
    row.append(cell);
    ordersBody.append(row);
    return;
  }

  for (const order of orders) {
    const row = document.createElement("tr");
    row.append(
      makeCell(order.orderId), makeCell(order.orderNo), makeCell(order.customerName)
    );

    const statusCell = document.createElement("td");
    const statusBadge = document.createElement("span");
    statusBadge.textContent = order.status;
    statusBadge.className = `pill ${["Pending", "Completed", "Cancelled"].includes(order.status) ? order.status.toLowerCase() : ""}`;
    statusCell.append(statusBadge);
    row.append(statusCell, makeCell(money.format(order.totalAmount), "numeric"));

    const actionCell = document.createElement("td");
    const action = document.createElement("button");
    action.type = "button";
    action.className = "button-secondary";
    action.textContent = "查看";
    action.addEventListener("click", () => loadDetail(order.orderId));
    actionCell.append(action);
    row.append(actionCell);
    ordersBody.append(row);
  }
}

async function loadOrders() {
  loadOrdersButton.disabled = true;
  setStatus("查詢中，正在向 C# API 取得資料…");
  try {
    const status = statusFilter.value;
    const url = status ? `/api/orders?status=${encodeURIComponent(status)}` : "/api/orders";
    const orders = await getJson(url);
    if (!Array.isArray(orders)) throw new Error("API 未傳回訂單陣列");
    renderOrders(orders);
    setStatus(`查詢完成：${orders.length} 筆訂單（${status || "全部"}）`);
  } catch (error) {
    renderOrders([]);
    setStatus(`無法載入訂單：${error.message}`, true);
  } finally {
    loadOrdersButton.disabled = false;
  }
}

async function loadDetail(orderId) {
  detailFields.hidden = true;
  detailFields.replaceChildren();
  detailPlaceholder.hidden = false;
  detailPlaceholder.textContent = `正在查詢訂單 #${orderId}…`;

  try {
    const order = await getJson(`/api/orders/${encodeURIComponent(orderId)}`);
    const fields = [
      ["訂單 ID", order.orderId],
      ["訂單編號", order.orderNo],
      ["客戶名稱", order.customerName],
      ["狀態", order.status],
      ["總金額", money.format(order.totalAmount)]
    ];
    for (const [name, value] of fields) {
      const wrapper = document.createElement("div");
      const dt = document.createElement("dt");
      const dd = document.createElement("dd");
      dt.textContent = name;
      dd.textContent = String(value ?? "");
      wrapper.append(dt, dd);
      detailFields.append(wrapper);
    }
    detailPlaceholder.hidden = true;
    detailFields.hidden = false;
  } catch (error) {
    detailPlaceholder.textContent = `無法查詢單筆訂單：${error.message}`;
  }
}

loadOrdersButton.addEventListener("click", loadOrders);
statusFilter.addEventListener("change", loadOrders);
loadOrders();
