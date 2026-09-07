document.addEventListener("DOMContentLoaded", function () {
  // Theme toggle initialization
  const themeToggleBtn = document.getElementById("theme-toggle-btn");
  if (themeToggleBtn) {
    const savedTheme = localStorage.getItem("openiddict-theme") || "dark";
    document.documentElement.setAttribute("data-theme", savedTheme);

    themeToggleBtn.addEventListener("click", function () {
      toggleTheme();
    });
  }

  // Restore sidebar collapsed state from localStorage
  const savedSidebar = localStorage.getItem("openiddict-sidebar-collapsed");
  if (savedSidebar === "true" && window.innerWidth > 768) {
    document.body.classList.add("sidebar-collapsed");
  }

  // Sidebar backdrop dismissal
  const sidebarBackdrop = document.getElementById("sidebar-backdrop");
  if (sidebarBackdrop) {
    sidebarBackdrop.addEventListener("click", function () {
      const sidebar = document.getElementById("sidebar");
      if (sidebar) sidebar.classList.remove("open");
      sidebarBackdrop.classList.remove("show");
    });
  }

  // Global keybindings
  document.addEventListener("keydown", function (e) {
    if (e.key === "Escape") {
      closeMobileSidebar();
      closeConfirmModal();
      closeTokenDrawer();
      closeCommandPalette();
      closeAllModals();
      closeAllDropdowns();
    }
    if ((e.metaKey || e.ctrlKey) && (e.key === "k" || e.key === "K")) {
      e.preventDefault();
      const palette = document.getElementById("command-palette-backdrop");
      if (palette && palette.classList.contains("show")) {
        closeCommandPalette();
      } else {
        openCommandPalette();
      }
    }
  });

  // 3-Dots Dropdown Menu Toggle Handler (Floating Positioned)
  document.addEventListener("click", function (e) {
    const trigger = e.target.closest(".dropdown-trigger");
    if (trigger) {
      toggleDropdown(trigger, e);
    } else if (!e.target.closest(".dropdown-menu")) {
      closeAllDropdowns();
    }
  });

  window.addEventListener("scroll", closeAllDropdowns, true);
  window.addEventListener("resize", closeAllDropdowns);

  // Dynamic Input Validation for Action Disabling
  document.addEventListener("input", function (e) {
    const input = e.target;
    if (input.dataset.btnTarget) {
      validateInputTarget(input);
    }
  });

  // Initialize all inputs with data-btn-target
  document.querySelectorAll("input[data-btn-target]").forEach((input) => {
    validateInputTarget(input);
  });

  // Server-rendered toast triggers (with deduplication)
  const renderedMessages = new Set();
  document.querySelectorAll(".server-toast-data").forEach((el) => {
    const msg = el.getAttribute("data-message");
    const type = el.getAttribute("data-type") || "success";
    const title = el.getAttribute("data-title");
    if (msg && !renderedMessages.has(msg)) {
      renderedMessages.add(msg);
      setTimeout(() => showToast(msg, type, title), 100);
    }
  });

  // Command palette input listeners
  const cmdInput = document.getElementById("command-palette-input");
  if (cmdInput) {
    cmdInput.addEventListener("input", filterCommandPalette);
    cmdInput.addEventListener("keydown", handleCommandKeyboard);
  }

  // Command item click handlers
  const cmdList = document.getElementById("command-palette-list");
  if (cmdList) {
    cmdList.addEventListener("click", function (e) {
      const item = e.target.closest(".command-item");
      if (item) {
        executeCommandItem(item);
      }
    });
  }

  // Auto-submit search inputs on debounce
  const searchInput = document.getElementById("search-input");
  if (searchInput) {
    let timeout = null;
    searchInput.addEventListener("input", function () {
      clearTimeout(timeout);
      timeout = setTimeout(function () {
        const form = searchInput.closest("form");
        if (form) {
          form.submit();
        }
      }, 500);
    });
  }
});

// Sidebar Toggle Function (Desktop Collapse + Mobile Drawer)
function toggleSidebar() {
  const sidebar = document.getElementById("sidebar");
  const sidebarBackdrop = document.getElementById("sidebar-backdrop");

  if (window.innerWidth <= 768) {
    if (sidebar) {
      const isOpen = sidebar.classList.toggle("open");
      if (sidebarBackdrop) {
        sidebarBackdrop.classList.toggle("show", isOpen);
      }
    }
  } else {
    const isCollapsed = document.body.classList.toggle("sidebar-collapsed");
    localStorage.setItem("openiddict-sidebar-collapsed", isCollapsed ? "true" : "false");
  }
}

function closeMobileSidebar() {
  const sidebar = document.getElementById("sidebar");
  const sidebarBackdrop = document.getElementById("sidebar-backdrop");
  if (sidebar) sidebar.classList.remove("open");
  if (sidebarBackdrop) sidebarBackdrop.classList.remove("show");
}

// Dropdown Toggle and Positioning Engine (Prevents table scroll clipping and overflow)
function toggleDropdown(trigger, event) {
  if (event) {
    event.stopPropagation();
  }
  const parentDropdown = trigger ? trigger.closest(".action-dropdown") : null;
  if (!parentDropdown) return;
  const wasOpen = parentDropdown.classList.contains("open");
  closeAllDropdowns();
  if (!wasOpen) {
    parentDropdown.classList.add("open");
    positionDropdown(parentDropdown);
  }
}
window.toggleDropdown = toggleDropdown;

function positionDropdown(dropdown) {
  const trigger = dropdown.querySelector(".dropdown-trigger");
  const menu = dropdown.querySelector(".dropdown-menu");
  if (!trigger || !menu) return;

  const rect = trigger.getBoundingClientRect();
  const spaceBelow = window.innerHeight - rect.bottom;
  const menuHeight = 220;
  const spaceAbove = rect.top;

  menu.style.position = "fixed";
  menu.style.right = (window.innerWidth - rect.right) + "px";
  menu.style.left = "auto";

  if (spaceBelow < menuHeight && spaceAbove > spaceBelow) {
    menu.style.top = "auto";
    menu.style.bottom = (window.innerHeight - rect.top + 6) + "px";
  } else {
    menu.style.bottom = "auto";
    menu.style.top = (rect.bottom + 6) + "px";
  }
}

// Dropdown & Modal Closers
function closeAllDropdowns() {
  document.querySelectorAll(".action-dropdown.open").forEach((dd) => dd.classList.remove("open"));
}

function openModal(modalId) {
  const modal = document.getElementById(modalId);
  if (modal) {
    modal.classList.add("show");
    const firstInput = modal.querySelector("input:not([type=hidden]), select");
    if (firstInput) {
      setTimeout(() => firstInput.focus(), 50);
    }
  }
}

function closeModal(modalId) {
  const modal = document.getElementById(modalId);
  if (modal) {
    modal.classList.remove("show");
  }
}

function closeAllModals() {
  document.querySelectorAll(".modal-backdrop.show").forEach((m) => m.classList.remove("show"));
}

function openBulkStatusModal() {
  openModal("bulk-status-modal");
}

function closeBulkStatusModal() {
  closeModal("bulk-status-modal");
}

// Input target validator helper
function validateInputTarget(input) {
  const targetBtn = document.getElementById(input.dataset.btnTarget);
  if (targetBtn) {
    const hasVal = Boolean(input.value && input.value.trim().length > 0);
    targetBtn.disabled = !hasVal;
  }
}

// Secret Rotation Modal Helpers
function openRotateSecretModal(appId, clientId) {
  const backdrop = document.getElementById("rotate-secret-backdrop");
  const idInput = document.getElementById("rotate-secret-app-id");
  const clientText = document.getElementById("rotate-secret-client-name");
  const secretInput = document.getElementById("rotate-secret-input");
  const submitBtn = document.getElementById("rotate-secret-submit-btn");

  if (!backdrop || !idInput) return;

  idInput.value = appId;
  if (clientText) clientText.innerText = clientId;
  if (secretInput) {
    secretInput.value = "";
    generateClientSecret("rotate-secret-input");
    if (submitBtn) submitBtn.disabled = false;
  }

  backdrop.classList.add("show");
}

function closeRotateSecretModal() {
  closeModal("rotate-secret-backdrop");
}

// Theme switcher
function toggleTheme() {
  const currentTheme = document.documentElement.getAttribute("data-theme") || "dark";
  const newTheme = currentTheme === "dark" ? "light" : "dark";
  document.documentElement.setAttribute("data-theme", newTheme);
  localStorage.setItem("openiddict-theme", newTheme);
  showToast(`Switched to ${newTheme} theme`, "info", "Theme Updated");
}

// Toast notification helper (Vibrant Popups with Title and Close)
function showToast(message, type = "info", title = null) {
  const container = document.getElementById("toast-container");
  if (!container) return;

  const toast = document.createElement("div");
  toast.className = `toast toast-${type}`;

  const iconText = type === "success" ? "✓" : (type === "danger" ? "✕" : "ℹ");
  const defaultTitle = type === "success" ? "Success!" : (type === "danger" ? "Operation Failed" : "Notification");
  const toastTitle = title || defaultTitle;

  if (type === "success" || type === "danger" || type === "warning") {
    toast.innerHTML = `
      <div class="toast-icon">${iconText}</div>
      <div style="flex: 1;">
        <div class="toast-title">${toastTitle}</div>
        <div class="toast-body">${message}</div>
      </div>
      <button type="button" style="background:none;border:none;color:rgba(255,255,255,0.7);cursor:pointer;font-size:1.15rem;line-height:1;margin-left:0.5rem;" onclick="this.closest('.toast').remove()">&times;</button>
    `;
  } else {
    toast.innerHTML = `
      <div style="flex: 1;">
        <div class="toast-title">${toastTitle}</div>
        <div class="toast-body" style="color: var(--text-secondary);">${message}</div>
      </div>
      <button type="button" style="background:none;border:none;color:var(--text-muted);cursor:pointer;font-size:1.15rem;line-height:1;margin-left:0.5rem;" onclick="this.closest('.toast').remove()">&times;</button>
    `;
  }

  container.appendChild(toast);

  setTimeout(() => {
    if (toast.parentElement) {
      toast.style.opacity = "0";
      toast.style.transform = "translateX(50px)";
      toast.style.transition = "opacity 0.3s ease, transform 0.3s ease";
      setTimeout(() => toast.remove(), 300);
    }
  }, 4500);
}

// Secret generator helper
function generateClientSecret(inputId) {
  const chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_+-=";
  let secret = "";
  const array = new Uint8Array(32);
  window.crypto.getRandomValues(array);
  for (let i = 0; i < array.length; i++) {
    secret += chars[array[i] % chars.length];
  }
  const input = document.getElementById(inputId);
  if (input) {
    input.value = secret;
    input.dispatchEvent(new Event("input", { bubbles: true }));
  }
}

// Confirmation dialog helper
function openConfirmModal(title, message, onConfirm) {
  const backdrop = document.getElementById("confirm-modal-backdrop");
  const titleEl = document.getElementById("confirm-modal-title");
  const messageEl = document.getElementById("confirm-modal-message");
  const confirmBtn = document.getElementById("confirm-modal-action-btn");

  if (!backdrop || !titleEl || !messageEl || !confirmBtn) return;

  titleEl.innerText = title;
  messageEl.innerText = message;
  backdrop.classList.add("show");

  const handleConfirm = function () {
    backdrop.classList.remove("show");
    confirmBtn.removeEventListener("click", handleConfirm);
    if (typeof onConfirm === "function") {
      onConfirm();
    }
  };

  confirmBtn.addEventListener("click", handleConfirm);
}

function closeConfirmModal() {
  closeModal("confirm-modal-backdrop");
}

// Command Palette (Linear Inspiration)
function openCommandPalette() {
  const backdrop = document.getElementById("command-palette-backdrop");
  const input = document.getElementById("command-palette-input");
  if (!backdrop || !input) return;

  backdrop.classList.add("show");
  input.value = "";
  filterCommandPalette();
  setTimeout(() => input.focus(), 50);
}

function closeCommandPalette() {
  closeModal("command-palette-backdrop");
}

function filterCommandPalette() {
  const input = document.getElementById("command-palette-input");
  const query = (input ? input.value : "").trim().toLowerCase();
  const items = document.querySelectorAll(".command-item");
  let firstVisible = null;

  items.forEach((item) => {
    const title = (item.getAttribute("data-title") || item.innerText).toLowerCase();
    const isMatch = !query || title.includes(query);
    item.style.display = isMatch ? "flex" : "none";
    item.classList.remove("active");
    if (isMatch && !firstVisible) {
      firstVisible = item;
    }
  });

  if (firstVisible) {
    firstVisible.classList.add("active");
  }

  const groups = document.querySelectorAll(".command-group-title");
  groups.forEach((group) => {
    let hasVisibleChildren = false;
    let sibling = group.nextElementSibling;
    while (sibling && !sibling.classList.contains("command-group-title")) {
      if (sibling.style.display !== "none") {
        hasVisibleChildren = true;
        break;
      }
      sibling = sibling.nextElementSibling;
    }
    group.style.display = hasVisibleChildren ? "block" : "none";
  });
}

function handleCommandKeyboard(e) {
  const visibleItems = Array.from(document.querySelectorAll(".command-item")).filter(
    (el) => el.style.display !== "none"
  );
  if (!visibleItems.length) return;

  const activeIndex = visibleItems.findIndex((el) => el.classList.contains("active"));

  if (e.key === "ArrowDown") {
    e.preventDefault();
    const nextIndex = (activeIndex + 1) % visibleItems.length;
    visibleItems.forEach((el) => el.classList.remove("active"));
    visibleItems[nextIndex].classList.add("active");
    visibleItems[nextIndex].scrollIntoView({ block: "nearest" });
  } else if (e.key === "ArrowUp") {
    e.preventDefault();
    const prevIndex = (activeIndex - 1 + visibleItems.length) % visibleItems.length;
    visibleItems.forEach((el) => el.classList.remove("active"));
    visibleItems[prevIndex].classList.add("active");
    visibleItems[prevIndex].scrollIntoView({ block: "nearest" });
  } else if (e.key === "Enter") {
    e.preventDefault();
    const current = visibleItems[activeIndex >= 0 ? activeIndex : 0];
    if (current) {
      executeCommandItem(current);
    }
  }
}

function executeCommandItem(item) {
  const url = item.getAttribute("data-url");
  const action = item.getAttribute("data-action");

  closeCommandPalette();

  if (action === "toggle-theme") {
    toggleTheme();
  } else if (url) {
    window.location.href = url;
  }
}

// Copy to Clipboard with Feedback
function copyToClipboard(text, event) {
  if (event) {
    event.stopPropagation();
  }

  navigator.clipboard.writeText(text).then(
    () => {
      showToast(`Copied "${text.length > 20 ? text.substring(0, 17) + "..." : text}" to clipboard`, "success", "Copied to Clipboard");
    },
    () => {
      showToast("Failed to copy to clipboard", "danger", "Copy Error");
    }
  );
}

// Collapsible filter panel toggle
function toggleFilterPanel() {
  const panel = document.getElementById("filter-panel");
  const toggleText = document.getElementById("filter-toggle-text");
  const chevron = document.getElementById("filter-chevron");

  if (!panel) return;

  const isExpanded = panel.classList.toggle("expanded");
  if (toggleText) {
    toggleText.innerText = isExpanded ? "Hide Filters" : "Show Filters";
  }
  if (chevron) {
    chevron.style.transform = isExpanded ? "rotate(180deg)" : "rotate(0deg)";
  }
}

function escapeHtml(text) {
  if (!text) return "";
  const div = document.createElement("div");
  div.textContent = text;
  return div.innerHTML;
}

// Slide-Over Token Detail Drawer helpers
function openTokenDrawer(token) {
  const drawer = document.getElementById("token-drawer");
  const backdrop = document.getElementById("token-drawer-backdrop");
  const body = document.getElementById("token-drawer-body");

  if (!drawer || !backdrop || !body) return;

  const statusBadge = token.isRevoked
    ? '<span class="badge badge-danger"><span class="status-dot status-dot-danger"></span>Revoked</span>'
    : (token.isExpired
      ? '<span class="badge badge-warning"><span class="status-dot status-dot-warning"></span>Expired</span>'
      : '<span class="badge badge-success"><span class="status-dot status-dot-active"></span>Valid / Active</span>');

  body.innerHTML = `
    <div class="token-detail-field">
      <span class="token-detail-label">Status</span>
      <div>${statusBadge}</div>
    </div>
    <div class="token-detail-field">
      <span class="token-detail-label">Token ID</span>
      <span class="token-detail-value" style="display: flex; align-items: center; justify-content: space-between;">
        <code>${token.id || '-'}</code>
        ${token.id ? `<button type="button" class="copy-btn" onclick="copyToClipboard('${token.id}', event)">Copy</button>` : ''}
      </span>
    </div>
    <div class="token-detail-field">
      <span class="token-detail-label">Reference ID</span>
      <span class="token-detail-value" style="display: flex; align-items: center; justify-content: space-between;">
        <code>${token.refId || 'None'}</code>
        ${token.refId ? `<button type="button" class="copy-btn" onclick="copyToClipboard('${token.refId}', event)">Copy</button>` : ''}
      </span>
    </div>
    <div class="token-detail-field">
      <span class="token-detail-label">Client Application</span>
      <span class="token-detail-value"><strong>${token.clientId || '-'}</strong> ${token.clientName ? '(' + token.clientName + ')' : ''}</span>
    </div>
    <div class="token-detail-field">
      <span class="token-detail-label">Subject / User ID</span>
      <span class="token-detail-value">${token.subject ? '<code>' + token.subject + '</code>' : '<span style="color: var(--text-muted)">[Client Credentials]</span>'}</span>
    </div>
    <div class="token-detail-field">
      <span class="token-detail-label">Token Type</span>
      <span class="token-detail-value"><span class="badge badge-secondary">${token.type || 'token'}</span></span>
    </div>
    <div class="token-detail-field">
      <span class="token-detail-label">Created At</span>
      <span class="token-detail-value">${token.created || '-'}</span>
    </div>
    <div class="token-detail-field">
      <span class="token-detail-label">Expires At</span>
      <span class="token-detail-value">${token.expires || 'Never'}</span>
    </div>
    ${token.revokedAt ? `
    <div class="token-detail-field">
      <span class="token-detail-label">Revoked At</span>
      <span class="token-detail-value" style="color: var(--danger-color);">${token.revokedAt}</span>
    </div>` : ''}
    ${token.serializedData ? `
    <div class="token-detail-field" style="flex-direction: column; align-items: flex-start; gap: 0.5rem;">
      <div style="display: flex; justify-content: space-between; width: 100%; align-items: center;">
        <span class="token-detail-label">Serialized Data</span>
        <button type="button" class="copy-btn" data-copy="${escapeHtml(token.serializedData)}" onclick="copyToClipboard(this.getAttribute('data-copy'), event)">Copy</button>
      </div>
      <div class="token-detail-value" style="width: 100%;">
        <pre style="max-height: 220px; overflow: auto; background: var(--bg-tertiary); padding: 0.6rem; border-radius: var(--radius-sm); font-size: 0.75rem; word-break: break-all; white-space: pre-wrap; margin: 0; border: 1px solid var(--border-color);"><code>${escapeHtml(token.serializedData)}</code></pre>
      </div>
    </div>` : ''}
  `;

  backdrop.classList.add("show");
  drawer.classList.add("show");
}

function closeTokenDrawer() {
  const drawer = document.getElementById("token-drawer");
  const backdrop = document.getElementById("token-drawer-backdrop");

  if (drawer) drawer.classList.remove("show");
  if (backdrop) backdrop.classList.remove("show");
}
