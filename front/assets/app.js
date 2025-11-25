const defaultBaseUrl = "http://localhost:5222";
const logoutButton = document.querySelector("#logout");
const userNameEl = document.querySelector("#user-name");
const userRoleEl = document.querySelector("#user-role");

const attendanceStartDateInput = document.querySelector("#attendance-start-date");
const attendanceEndDateInput = document.querySelector("#attendance-end-date");
const attendanceForm = document.querySelector("#attendance-form");
const attendanceStatus = document.querySelector("#attendance-status");
const attendanceTitle = document.querySelector("#attendance-title");
const clockInBtn = document.querySelector("#clock-in-btn");
const clockOutBtn = document.querySelector("#clock-out-btn");

const leaveRequestForm = document.querySelector("#leave-request-form");
const leaveRequestStatus = document.querySelector("#leave-request-status");
const leaveTitle = document.querySelector("#leave-title");

const employeesTable = document.querySelector("#employees-table tbody");
const departmentsTable = document.querySelector("#departments-table tbody");
const attendancesTable = document.querySelector("#attendances-table tbody");
const leaveRequestsTable = document.querySelector("#leave-requests-table tbody");

const form = document.querySelector("#employee-form");
const formStatus = document.querySelector("#form-status");

const state = {
  baseUrl: localStorage.getItem("sge:baseUrl") || defaultBaseUrl,
  accessToken: localStorage.getItem("sge:accessToken") || "",
  refreshToken: localStorage.getItem("sge:refreshToken") || "",
  user: JSON.parse(localStorage.getItem("sge:user") || "null"),
};

// Check authentication on page load
function checkAuthentication() {
  if (!state.accessToken) {
    window.location.href = "./login.html";
    return false;
  }
  return true;
}

// Check if user has a specific role
function hasRole(role) {
  if (!state.user || !state.user.roles) return false;
  return state.user.roles.includes(role);
}

// Check if user has any of the required roles
function hasAnyRole(roles) {
  if (!Array.isArray(roles)) roles = [roles];
  return roles.some(role => hasRole(role));
}

// Set up role-based visibility
function setupRoleBasedVisibility() {
  const sections = document.querySelectorAll("[data-role-required]");
  sections.forEach(section => {
    const requiredRoles = section.getAttribute("data-role-required").split(",");
    if (!hasAnyRole(requiredRoles)) {
      section.style.display = "none";
    }
  });

  // Update UI based on role
  if (hasRole("Admin") || hasRole("Manager")) {
    attendanceTitle.textContent = "All attendances";
  } else {
    attendanceTitle.textContent = "My attendances";
    leaveTitle.textContent = "My leave requests";
  }
}

// Set default attendance dates to current month
function setDefaultDateRange() {
  const today = new Date();
  const firstDay = new Date(today.getFullYear(), today.getMonth(), 1);
  const lastDay = new Date(today.getFullYear(), today.getMonth() + 1, 0);
  
  attendanceStartDateInput.value = firstDay.toISOString().split("T")[0];
  attendanceEndDateInput.value = lastDay.toISOString().split("T")[0];
}

function persistAuth() {
  if (state.accessToken) {
    localStorage.setItem("sge:accessToken", state.accessToken);
    localStorage.setItem("sge:refreshToken", state.refreshToken);
    localStorage.setItem("sge:user", JSON.stringify(state.user));
  }
}

function clearAuth() {
  state.accessToken = "";
  state.refreshToken = "";
  state.user = null;
  localStorage.removeItem("sge:accessToken");
  localStorage.removeItem("sge:refreshToken");
  localStorage.removeItem("sge:user");
  updateUserLabel();
}

function handleAuthError(error, targetTable, roleHint = "") {
  const baseMessage = roleHint
    ? `${roleHint}. Connectez-vous avec un compte autorisé.`
    : "Authentification requise. Connectez-vous pour continuer.";

  if (error.status === 401) {
    clearAuth();
    window.location.href = "./login.html";
  }

  if (targetTable) {
    setPlaceholder(targetTable, baseMessage);
  } else {
    setStatus(formStatus, baseMessage, false);
  }
}

function setStatus(element, message, success = true) {
  if (!element) return;
  element.textContent = message;
  element.style.color = success ? "#15803d" : "#b91c1c";
}

function updateUserLabel() {
  if (state.user) {
    userNameEl.textContent = `${state.user.firstName} ${state.user.lastName}`;
    const role = state.user.roles?.length ? state.user.roles[0] : "User";
    userRoleEl.textContent = role;
  } else {
    userNameEl.textContent = "--";
    userRoleEl.textContent = "--";
  }
}

function setPlaceholder(tableEl, message) {
  tableEl.innerHTML = `<tr class="placeholder"><td colspan="${tableEl.parentElement.querySelectorAll("th").length}">${message}</td></tr>`;
}

function ensureAuthenticated(tableEl) {
  if (!state.accessToken) {
    setPlaceholder(tableEl, "Connectez-vous pour consulter ces données.");
    return false;
  }
  return true;
}

async function apiFetch(path, options = {}) {
  const url = path.startsWith("http") ? path : `${state.baseUrl}${path}`;
  const headers = new Headers(options.headers || {});

  if (state.accessToken) {
    headers.set("Authorization", `Bearer ${state.accessToken}`);
  }

  headers.set("Accept", "application/json");

  let body = options.body;
  if (body && !(body instanceof FormData) && typeof body !== "string") {
    body = JSON.stringify(body);
  }

  if (body && !(body instanceof FormData) && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  const response = await fetch(url, { ...options, headers, body });
  const contentType = response.headers.get("content-type") || "";
  const isJson = contentType.includes("application/json");
  const payload = isJson ? await response.json() : await response.text();

  if (!response.ok) {
    const message =
      typeof payload === "string" ? payload : payload?.message || payload?.error || JSON.stringify(payload);
    const error = new Error(message || `Request failed with status ${response.status}`);
    error.status = response.status;
    if (response.status === 401 || response.status === 403) {
      error.isAuthError = true;
    }
    throw error;
  }

  return payload;
}

async function loadEmployees() {
  try {
    setPlaceholder(employeesTable, "Chargement...");
    if (!ensureAuthenticated(employeesTable)) return;

    const employees = await apiFetch(`/api/Employees`);
    if (!employees.length) {
      setPlaceholder(employeesTable, "Aucun employé trouvé.");
      return;
    }

    employeesTable.innerHTML = employees
      .map(
        (emp) => `
          <tr>
            <td>${emp.id}</td>
            <td>${emp.fullName}</td>
            <td>${emp.email}</td>
            <td>${emp.departmentName}</td>
            <td>${emp.position}</td>
            <td>${emp.salary}</td>
          </tr>
        `,
      )
      .join("");
  } catch (error) {
    if (error.isAuthError) {
      handleAuthError(error, employeesTable, "Accès refusé (Admin ou Manager requis)");
      return;
    }
    setPlaceholder(employeesTable, `Erreur lors du chargement: ${error.message}`);
  }
}

async function loadDepartments() {
  try {
    setPlaceholder(departmentsTable, "Chargement...");
    if (!ensureAuthenticated(departmentsTable)) return;

    const departments = await apiFetch(`/api/Departments`);
    if (!departments.length) {
      setPlaceholder(departmentsTable, "Aucun département trouvé.");
      return;
    }

    departmentsTable.innerHTML = departments
      .map(
        (dept) => `
          <tr>
            <td>${dept.id}</td>
            <td>${dept.name}</td>
            <td>${dept.description ?? ""}</td>
          </tr>
        `,
      )
      .join("");
  } catch (error) {
    if (error.isAuthError) {
      handleAuthError(error, departmentsTable, "Accès refusé (Admin ou Manager requis)");
      return;
    }
    setPlaceholder(departmentsTable, `Erreur lors du chargement: ${error.message}`);
  }
}

async function loadAttendances() {
  try {
    setPlaceholder(attendancesTable, "Chargement...");
    if (!ensureAuthenticated(attendancesTable)) return;

    let attendances;
    const startDate = attendanceStartDateInput?.value || new Date().toISOString().split("T")[0];
    const endDate = attendanceEndDateInput?.value || new Date().toISOString().split("T")[0];

    if (hasRole("Admin") || hasRole("Manager")) {
      // Admin/Manager: Get all attendances for specific date
      const date = startDate;
      attendances = await apiFetch(`/api/Attendances/date/${date}`);
    } else {
      // Regular user: Get their own attendances for date range
      const employeeId = state.user?.employeeId || state.user?.id;
      if (!employeeId) {
        setPlaceholder(attendancesTable, "Impossible de récupérer vos présences.");
        return;
      }
      const params = new URLSearchParams({
        startDate: `${startDate}T00:00:00Z`,
        endDate: `${endDate}T23:59:59Z`
      });
      attendances = await apiFetch(`/api/Attendances/employee/${employeeId}?${params}`);
    }
    
    if (!attendances || !Array.isArray(attendances) || attendances.length === 0) {
      setPlaceholder(attendancesTable, "Aucune présence trouvée.");
      return;
    }

    attendancesTable.innerHTML = attendances
      .map(
        (att) => `
          <tr>
            <td>${att.id}</td>
            <td>${att.employeeId}</td>
            <td>${att.date?.split("T")[0] ?? ""}</td>
            <td>${att.clockIn ?? "-"} / ${att.clockOut ?? "-"}</td>
            <td>${att.notes ?? ""}</td>
          </tr>
        `,
      )
      .join("");
  } catch (error) {
    if (error.isAuthError) {
      handleAuthError(error, attendancesTable, "Accès refusé");
      return;
    }
    setPlaceholder(attendancesTable, `Erreur lors du chargement: ${error.message}`);
  }
}

form.addEventListener("submit", async (event) => {
  event.preventDefault();
  const data = new FormData(form);
  const payload = Object.fromEntries(data.entries());

  payload.salary = Number(payload.salary);
  payload.departmentId = Number(payload.departmentId);

  if (!state.accessToken) {
    setStatus(formStatus, "Connectez-vous avec un compte Admin pour créer un employé.", false);
    return;
  }

  try {
    setStatus(formStatus, "Envoi en cours...");
    await apiFetch(`/api/Employees`, {
      method: "POST",
      body: payload,
    });

    setStatus(formStatus, "Employé créé avec succès.");
    form.reset();
    loadEmployees();
  } catch (error) {
    if (error.isAuthError) {
      handleAuthError(error, null, "Accès refusé (rôle Admin requis)");
      return;
    }
    setStatus(formStatus, `Erreur: ${error.message}`, false);
  }
});

logoutButton.addEventListener("click", () => {
  clearAuth();
  window.location.href = "./login.html";
});

// Attendance tracking
clockInBtn?.addEventListener("click", async () => {
  const employeeId = attendanceForm?.querySelector('input[name="employeeId"]')?.value;
  const notes = attendanceForm?.querySelector('input[name="notes"]')?.value;

  if (!state.accessToken) {
    setStatus(attendanceStatus, "Connectez-vous pour pointer.", false);
    return;
  }

  try {
    setStatus(attendanceStatus, "Envoi en cours...");
    await apiFetch(`/api/Attendances/clock-in`, {
      method: "POST",
      body: {
        employeeId: Number(employeeId),
        dateTime: new Date().toISOString(),
        notes: notes || "",
      },
    });

    setStatus(attendanceStatus, "Pointage d'arrivée enregistré.", true);
    attendanceForm?.reset();
    loadAttendances();
  } catch (error) {
    setStatus(attendanceStatus, `Erreur: ${error.message}`, false);
  }
});

clockOutBtn?.addEventListener("click", async () => {
  const employeeId = attendanceForm?.querySelector('input[name="employeeId"]')?.value;
  const notes = attendanceForm?.querySelector('input[name="notes"]')?.value;

  if (!state.accessToken) {
    setStatus(attendanceStatus, "Connectez-vous pour pointer.", false);
    return;
  }

  try {
    setStatus(attendanceStatus, "Envoi en cours...");
    await apiFetch(`/api/Attendances/clock-out`, {
      method: "POST",
      body: {
        employeeId: Number(employeeId),
        dateTime: new Date().toISOString(),
        notes: notes || "",
      },
    });

    setStatus(attendanceStatus, "Pointage de départ enregistré.", true);
    attendanceForm?.reset();
    loadAttendances();
  } catch (error) {
    setStatus(attendanceStatus, `Erreur: ${error.message}`, false);
  }
});

// Leave requests
leaveRequestForm?.addEventListener("submit", async (event) => {
  event.preventDefault();
  const data = new FormData(leaveRequestForm);
  const payload = {
    employeeId: Number(data.get("employeeId")),
    leaveType: Number(data.get("leaveType")),
    startDate: new Date(data.get("startDate")).toISOString(),
    endDate: new Date(data.get("endDate")).toISOString(),
    reason: data.get("reason"),
  };

  if (!state.accessToken) {
    setStatus(leaveRequestStatus, "Connectez-vous pour créer une demande.", false);
    return;
  }

  try {
    setStatus(leaveRequestStatus, "Envoi en cours...");
    await apiFetch(`/api/LeaveRequests`, {
      method: "POST",
      body: payload,
    });

    setStatus(leaveRequestStatus, "Demande de congé créée avec succès.", true);
    leaveRequestForm.reset();
    loadLeaveRequests();
  } catch (error) {
    setStatus(leaveRequestStatus, `Erreur: ${error.message}`, false);
  }
});

async function loadLeaveRequests() {
  try {
    setPlaceholder(leaveRequestsTable, "Chargement...");
    if (!ensureAuthenticated(leaveRequestsTable)) return;

    let leaveRequests;
    
    if (hasRole("Admin") || hasRole("Manager")) {
      // Admin/Manager: Get all pending requests
      leaveRequests = await apiFetch(`/api/LeaveRequests/pending`);
    } else {
      // Regular user: Get their own leave requests
      const employeeId = state.user?.employeeId || state.user?.id;
      if (!employeeId) {
        setPlaceholder(leaveRequestsTable, "Impossible de récupérer vos demandes de congé.");
        return;
      }
      leaveRequests = await apiFetch(`/api/LeaveRequests/employee/${employeeId}`);
    }
    
    if (!leaveRequests || !Array.isArray(leaveRequests) || leaveRequests.length === 0) {
      setPlaceholder(leaveRequestsTable, "Aucune demande de congé trouvée.");
      return;
    }

    const leaveTypeMap = { 1: "Annual", 2: "Sick", 3: "Maternity", 4: "Paternity", 5: "Personal", 6: "Unpaid" };
    const statusMap = { 1: "Pending", 2: "Approved", 3: "Rejected", 4: "Cancelled" };

    leaveRequestsTable.innerHTML = leaveRequests
      .map(
        (req) => `
          <tr>
            <td>${req.id}</td>
            <td>${req.employeeId}</td>
            <td>${leaveTypeMap[req.leaveType] || "Unknown"}</td>
            <td>${req.startDate?.split("T")[0] ?? ""}</td>
            <td>${req.endDate?.split("T")[0] ?? ""}</td>
            <td>${statusMap[req.status] || "Unknown"}</td>
            <td>${req.reason ?? ""}</td>
          </tr>
        `,
      )
      .join("");
  } catch (error) {
    if (error.isAuthError) {
      handleAuthError(error, leaveRequestsTable, "Accès refusé");
      return;
    }
    setPlaceholder(leaveRequestsTable, `Erreur lors du chargement: ${error.message}`);
  }
}

// Event listeners for date filtering
attendanceStartDateInput?.addEventListener("change", loadAttendances);
attendanceEndDateInput?.addEventListener("change", loadAttendances);

// Refresh buttons
document.querySelector('[data-action="refresh-employees"]')?.addEventListener("click", loadEmployees);
document.querySelector('[data-action="refresh-departments"]')?.addEventListener("click", loadDepartments);
document.querySelector('[data-action="refresh-attendances"]')?.addEventListener("click", loadAttendances);
document.querySelector('[data-action="refresh-leave-requests"]')?.addEventListener("click", loadLeaveRequests);

// Check authentication on page load
if (!checkAuthentication()) {
  throw new Error("Redirecting to login...");
}

// Initialize user display
updateUserLabel();

// Set up role-based visibility
setupRoleBasedVisibility();

// Set default date range for attendance
setDefaultDateRange();

// Load all data
loadEmployees();
loadDepartments();
loadAttendances();
loadLeaveRequests();