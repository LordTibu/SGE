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
const roleRoutesContainer = document.querySelector("#role-routes");

const todayAttendanceForm = document.querySelector("#today-attendance-form");
const todayAttendanceStatus = document.querySelector("#today-attendance-status");
const monthlyHoursForm = document.querySelector("#monthly-hours-form");
const monthlyHoursStatus = document.querySelector("#monthly-hours-status");
const manualAttendanceForm = document.querySelector("#manual-attendance-form");
const manualAttendanceStatus = document.querySelector("#manual-attendance-status");

const dailyEmployeeAttendanceForm = document.querySelector("#daily-employee-attendance-form");
const dailyEmployeeAttendanceStatus = document.querySelector("#daily-employee-attendance-status");
const dailyEmployeeAttendanceTable = document.querySelector("#daily-employee-attendance-table tbody");

const attendanceByDateForm = document.querySelector("#attendance-by-date-form");
const attendanceByDateStatus = document.querySelector("#attendance-by-date-status");
const attendanceByDateTable = document.querySelector("#attendance-by-date-table tbody");

const leaveStatusFilterForm = document.querySelector("#leave-status-filter-form");
const leaveStatusFilterStatus = document.querySelector("#leave-status-filter");
const leaveTypeFilterForm = document.querySelector("#leave-type-filter-form");
const leaveTypeFilterStatus = document.querySelector("#leave-type-filter-status");
const leaveUpdateForm = document.querySelector("#leave-update-form");
const leaveUpdateStatus = document.querySelector("#leave-update-status");
const leaveRemainingForm = document.querySelector("#leave-remaining-form");
const leaveRemainingStatus = document.querySelector("#leave-remaining-status");
const leaveConflictForm = document.querySelector("#leave-conflict-form");
const leaveConflictStatus = document.querySelector("#leave-conflict-status");

const leaveTypeMap = { 1: "Annual", 2: "Sick", 3: "Maternity", 4: "Paternity", 5: "Personal", 6: "Unpaid" };
const leaveStatusMap = { 1: "Pending", 2: "Approved", 3: "Rejected", 4: "Cancelled" };

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

const roleRoutes = {
  Admin: [
    { label: "Lister les employés", route: "/api/Employees", hint: "Vue globale RH" },
    { label: "Gérer les départements", route: "/api/Departments", hint: "Structurer l'organisation" },
    { label: "Créer un pointage manuel", route: "/api/Attendances", hint: "Corriger ou compléter" },
    { label: "Approver les congés", route: "/api/LeaveRequests/status/{status}", hint: "Valider les demandes" },
  ],
  Manager: [
    { label: "Présences de l'équipe", route: "/api/Attendances/date/{date}", hint: "Contrôle quotidien" },
    { label: "Demandes en attente", route: "/api/LeaveRequests/pending", hint: "Décider rapidement" },
    { label: "Mettre à jour un congé", route: "/api/LeaveRequests/{id}/status", hint: "Approuver/Rejeter" },
    { label: "Créer un pointage manuel", route: "/api/Attendances", hint: "Saisies correctives" },
  ],
  User: [
    { label: "Mes présences", route: "/api/Attendances/employee/{id}", hint: "Historique personnel" },
    { label: "Présence du jour", route: "/api/Attendances/employee/{id}/today", hint: "Suivi quotidien" },
    { label: "Demander un congé", route: "/api/LeaveRequests", hint: "Soumettre une absence" },
    { label: "Solde de congés", route: "/api/LeaveRequests/employee/{id}/remaining/{year}", hint: "Vérifier mes droits" },
  ],
};

function checkAuthentication() {
  if (!state.accessToken) {
    window.location.href = "./login.html";
    return false;
  }
  return true;
}

function hasRole(role) {
  if (!state.user || !state.user.roles) return false;

  const normalize = (r) =>
    String(r || "")
      .trim()
      .toLowerCase();

  const normalizedRoles = new Set(state.user.roles.map(normalize));

  if (Array.isArray(role)) {
    return role.some((r) => normalizedRoles.has(normalize(r)));
  }

  return normalizedRoles.has(normalize(role));
}

function hasAnyRole(roles) {
  if (!Array.isArray(roles)) roles = [roles];
  return roles.some(role => hasRole(role));
}

function renderRoleRoutes() {
  if (!roleRoutesContainer) return;
  const userRoles = state.user?.roles?.length ? state.user.roles : ["User"];
  const seen = new Set();
  const tiles = userRoles
    .map((role) => {
      const routes = roleRoutes[role] || [];
      return routes
        .filter((r) => {
          const key = `${role}-${r.route}`;
          if (seen.has(key)) return false;
          seen.add(key);
          return true;
        })
        .map(
          (route) => `
            <div class="role-pill" data-role="${role}">
              <span class="dot" aria-hidden="true"></span>
              <div>
                <div>${route.label}</div>
                <small>${route.route} · ${route.hint}</small>
              </div>
            </div>
          `,
        )
        .join("");
    })
    .join("");

  roleRoutesContainer.innerHTML = tiles;
}

function setupRoleBasedVisibility() {
  const sections = document.querySelectorAll("[data-role-required]");
  sections.forEach(section => {
    const requiredRoles = section.getAttribute("data-role-required").split(",");
    if (!hasAnyRole(requiredRoles)) {
      section.style.display = "none";
    }
  });

  if (hasRole("Admin") || hasRole("Manager")) {
    attendanceTitle.textContent = "All attendances";
  } else {
    attendanceTitle.textContent = "My attendances";
    leaveTitle.textContent = "My leave requests";
  }
}

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

function getEmployeeIdFromState() {
  return state.user?.employeeId;
}

function prefillEmployeeIds() {
  const employeeId = getEmployeeIdFromState();
  if (!employeeId) return;

  const selectors = [
    '#attendance-form input[name="employeeId"]',
    '#leave-request-form input[name="employeeId"]',
    '#today-attendance-form input[name="employeeId"]',
    '#monthly-hours-form input[name="employeeId"]',
    '#manual-attendance-form input[name="employeeId"]',
    '#leave-remaining-form input[name="employeeId"]',
    '#leave-conflict-form input[name="employeeId"]',
  ];

  selectors.forEach((selector) => {
    const input = document.querySelector(selector);
    if (input && !input.value) {
      input.value = employeeId;
    }
  });
}

function setPlaceholder(tableEl, message) {
  tableEl.innerHTML = `<tr class="placeholder"><td colspan="${tableEl.parentElement.querySelectorAll("th").length}">${message}</td></tr>`;
}

function ensureAuthenticated(tableEl) {
  if (!state.accessToken) {
    if (tableEl) setPlaceholder(tableEl, "Connectez-vous pour consulter ces données.");
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
      const date = startDate;
      attendances = await apiFetch(`/api/Attendances/date/${date}`);
    } else {
      const employeeId = getEmployeeIdFromState();
      if (!employeeId) {
        setPlaceholder(attendancesTable, "Impossible de récupérer vos présences.");
        return;
      }
      const params = new URLSearchParams({
        startDate: `${startDate}T00:00:00Z`,
        endDate: `${endDate}T23:59:59Z`,
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

function toTimeSpan(timeValue) {
  if (!timeValue) return null;
  return timeValue.length === 5 ? `${timeValue}:00` : timeValue;
}

async function loadTodayAttendance(employeeId) {
  setStatus(todayAttendanceStatus, "Vérification en cours...");
  try {
    const attendance = await apiFetch(`/api/Attendances/employee/${employeeId}/today`);
    const timeInfo = attendance
      ? `${attendance.clockIn ?? "-"} / ${attendance.clockOut ?? "-"}`
      : "Aucun pointage aujourd'hui";
    setStatus(todayAttendanceStatus, `Présence du jour: ${timeInfo}`);
  } catch (error) {
    setStatus(todayAttendanceStatus, `Erreur: ${error.message}`, false);
  }
}

async function loadMonthlyHours(employeeId, year, month) {
  setStatus(monthlyHoursStatus, "Calcul en cours...");
  try {
    const totalHours = await apiFetch(`/api/Attendances/employee/${employeeId}/hours/${year}/${month}`);
    setStatus(monthlyHoursStatus, `${totalHours} heures cumulées`);
  } catch (error) {
    setStatus(monthlyHoursStatus, `Erreur: ${error.message}`, false);
  }
}

async function loadDailyEmployeeAttendance(employeeId) {
  setStatus(dailyEmployeeAttendanceStatus, "Chargement en cours...");
  try {
    const attendance = await apiFetch(`/api/Attendances/employee/${employeeId}/today`);
    
    if (!attendance) {
      setPlaceholder(dailyEmployeeAttendanceTable, "Aucune présence trouvée pour aujourd'hui.");
      setStatus(dailyEmployeeAttendanceStatus, "Aucune présence trouvée.");
      return;
    }

    dailyEmployeeAttendanceTable.innerHTML = `
      <tr>
        <td>${attendance.id}</td>
        <td>${attendance.employeeId}</td>
        <td>${attendance.date?.split("T")[0] ?? ""}</td>
        <td>${attendance.clockIn ?? "-"}</td>
        <td>${attendance.clockOut ?? "-"}</td>
        <td>${attendance.notes ?? ""}</td>
      </tr>
    `;
    setStatus(dailyEmployeeAttendanceStatus, "Présence chargée.", true);
  } catch (error) {
    setStatus(dailyEmployeeAttendanceStatus, `Erreur: ${error.message}`, false);
    setPlaceholder(dailyEmployeeAttendanceTable, `Erreur: ${error.message}`);
  }
}

async function loadAttendancesByDate(dateString) {
  setStatus(attendanceByDateStatus, "Chargement en cours...");
  try {
    const attendances = await apiFetch(`/api/Attendances/date/${dateString}`);
    
    if (Array.isArray(attendances) && attendances.length > 0) {
      attendanceByDateTable.innerHTML = attendances.map(attendance => `
        <tr>
          <td>${attendance.id}</td>
          <td>${attendance.employeeId}</td>
          <td>${attendance.date?.split("T")[0] ?? ""}</td>
          <td>${attendance.clockIn ?? "-"}</td>
          <td>${attendance.clockOut ?? "-"}</td>
          <td>${attendance.notes ?? ""}</td>
        </tr>
      `).join("");
      setStatus(attendanceByDateStatus, `${attendances.length} attendance(s) chargée(s).`, true);
    } else {
      setPlaceholder(attendanceByDateTable, "Aucune présence trouvée pour cette date.");
      setStatus(attendanceByDateStatus, "Aucune présence trouvée.", true);
    }
  } catch (error) {
    setStatus(attendanceByDateStatus, `Erreur: ${error.message}`, false);
    setPlaceholder(attendanceByDateTable, `Erreur: ${error.message}`);
  }
}

async function createManualAttendance(payload) {
  setStatus(manualAttendanceStatus, "Envoi en cours...");
  try {
    await apiFetch(`/api/Attendances`, {
      method: "POST",
      body: payload,
    });
    setStatus(manualAttendanceStatus, "Pointage manuel créé.");
    manualAttendanceForm?.reset();
    prefillEmployeeIds();
    loadAttendances();
  } catch (error) {
    setStatus(manualAttendanceStatus, `Erreur: ${error.message}`, false);
  }
}

form.addEventListener("submit", async (event) => {
  event.preventDefault();
  const data = new FormData(form);
  const payload = Object.fromEntries(data.entries());

  payload.salary = Number(payload.salary);
  payload.departmentId = Number(payload.departmentId);
  
  // Convert hireDate to ISO 8601 format (YYYY-MM-DDTHH:mm:ssZ)
  const [year, month, day] = payload.hireDate.split('-');
  payload.hireDate = `${year}-${month}-${day}T00:00:00Z`;

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
    prefillEmployeeIds();
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
    prefillEmployeeIds();
    loadAttendances();
  } catch (error) {
    setStatus(attendanceStatus, `Erreur: ${error.message}`, false);
  }
});

todayAttendanceForm?.addEventListener("submit", async (event) => {
  event.preventDefault();
  if (!ensureAuthenticated()) {
    setStatus(todayAttendanceStatus, "Connectez-vous pour consulter vos présences.", false);
    return;
  }
  const employeeId = todayAttendanceForm.querySelector('input[name="employeeId"]')?.value;
  await loadTodayAttendance(Number(employeeId));
});

monthlyHoursForm?.addEventListener("submit", async (event) => {
  event.preventDefault();
  if (!ensureAuthenticated()) {
    setStatus(monthlyHoursStatus, "Connectez-vous pour consulter vos heures.", false);
    return;
  }
  const employeeId = monthlyHoursForm.querySelector('input[name="employeeId"]')?.value;
  const month = monthlyHoursForm.querySelector('input[name="month"]')?.value;
  const year = monthlyHoursForm.querySelector('input[name="year"]')?.value;
  await loadMonthlyHours(Number(employeeId), Number(year), Number(month));
});

dailyEmployeeAttendanceForm?.addEventListener("submit", async (event) => {
  event.preventDefault();
  if (!ensureAuthenticated()) {
    setStatus(dailyEmployeeAttendanceStatus, "Connectez-vous pour consulter la présence.", false);
    return;
  }
  const employeeId = dailyEmployeeAttendanceForm.querySelector('input[name="employeeId"]')?.value;
  await loadDailyEmployeeAttendance(Number(employeeId));
});

attendanceByDateForm?.addEventListener("submit", async (event) => {
  event.preventDefault();
  if (!ensureAuthenticated()) {
    setStatus(attendanceByDateStatus, "Connectez-vous pour consulter la présence.", false);
    return;
  }
  const date = attendanceByDateForm.querySelector('input[name="attendanceDate"]')?.value;
  if (date) {
    await loadAttendancesByDate(date);
  }
});

manualAttendanceForm?.addEventListener("submit", async (event) => {
  event.preventDefault();
  if (!state.accessToken) {
    setStatus(manualAttendanceStatus, "Connectez-vous avec un rôle autorisé.", false);
    return;
  }

  const data = new FormData(manualAttendanceForm);
  const payload = {
    employeeId: Number(data.get("employeeId")),
    date: new Date(data.get("date")).toISOString(),
    clockIn: toTimeSpan(data.get("clockIn")),
    clockOut: toTimeSpan(data.get("clockOut")),
    breakDurationHours: Number(data.get("breakDurationHours") || 0),
    notes: data.get("notes") || "",
  };

  await createManualAttendance(payload);
});

const leaveRequestForm = document.querySelector("#leave-request-form");
const leaveRequestStatus = document.querySelector("#leave-request-status");
const leaveTitle = document.querySelector("#leave-title");

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
    prefillEmployeeIds();
    loadLeaveRequests();
  } catch (error) {
    setStatus(leaveRequestStatus, `Erreur: ${error.message}`, false);
  }
});

function renderLeaveRequestsTable(leaveRequests) {
  if (!leaveRequests || !Array.isArray(leaveRequests) || leaveRequests.length === 0) {
    setPlaceholder(leaveRequestsTable, "Aucune demande de congé trouvée.");
    return;
  }

  leaveRequestsTable.innerHTML = leaveRequests
    .map(
      (req) => `
        <tr>
          <td>${req.id}</td>
          <td>${req.employeeId}</td>
          <td>${leaveTypeMap[req.leaveType] || "Unknown"}</td>
          <td>${req.startDate?.split("T")[0] ?? ""}</td>
          <td>${req.endDate?.split("T")[0] ?? ""}</td>
          <td>${leaveStatusMap[req.status] || "Unknown"}</td>
          <td>${req.reason ?? ""}</td>
        </tr>
      `,
    )
    .join("");
}

async function loadLeaveRequests() {
  try {
    setPlaceholder(leaveRequestsTable, "Chargement...");
    if (!ensureAuthenticated(leaveRequestsTable)) return;

    let leaveRequests;

    if (hasRole("Admin") || hasRole("Manager")) {
      leaveRequests = await apiFetch(`/api/LeaveRequests/pending`);
    } else {
      const employeeId = getEmployeeIdFromState();
      if (!employeeId) {
        setPlaceholder(leaveRequestsTable, "Impossible de récupérer vos demandes de congé.");
        return;
      }
      leaveRequests = await apiFetch(`/api/LeaveRequests/employee/${employeeId}`);
    }

    renderLeaveRequestsTable(leaveRequests);
  } catch (error) {
    if (error.isAuthError) {
      handleAuthError(error, leaveRequestsTable, "Accès refusé");
      return;
    }
    setPlaceholder(leaveRequestsTable, `Erreur lors du chargement: ${error.message}`);
  }
}

leaveStatusFilterForm?.addEventListener("submit", async (event) => {
  event.preventDefault();
  if (!state.accessToken) {
    setStatus(leaveStatusFilterStatus, "Connectez-vous avec un compte autorisé.", false);
    return;
  }

  const statusValue = leaveStatusFilterForm.querySelector("select[name='status']")?.value;
  setStatus(leaveStatusFilterStatus, "Chargement...");
  try {
    const leaveRequests = await apiFetch(`/api/LeaveRequests/status/${statusValue}`);
    renderLeaveRequestsTable(leaveRequests);
    setStatus(leaveStatusFilterStatus, "Filtre appliqué.");
  } catch (error) {
    setStatus(leaveStatusFilterStatus, `Erreur: ${error.message}`, false);
  }
});

leaveTypeFilterForm?.addEventListener("submit", async (event) => {
  event.preventDefault();
  if (!state.accessToken) {
    setStatus(leaveTypeFilterStatus, "Connectez-vous avec un compte autorisé.", false);
    return;
  }

  const leaveTypeValue = leaveTypeFilterForm.querySelector("select[name='leaveType']")?.value;
  setStatus(leaveTypeFilterStatus, "Chargement...");
  try {
    let leaveRequests;
    if (leaveTypeValue === "") {
      // Load all pending requests if "Tous" is selected
      leaveRequests = await apiFetch(`/api/LeaveRequests/pending`);
    } else {
      // Filter by type using status endpoint then filter client-side by type
      // Since we might not have a direct API endpoint for type filtering,
      // we'll fetch pending requests and filter by type
      const allRequests = await apiFetch(`/api/LeaveRequests/pending`);
      leaveRequests = allRequests.filter(req => String(req.leaveType) === leaveTypeValue);
    }
    renderLeaveRequestsTable(leaveRequests);
    const typeLabel = leaveTypeMap[leaveTypeValue] || "All types";
    setStatus(leaveTypeFilterStatus, `${leaveRequests.length} request(s) of type ${typeLabel}.`);
  } catch (error) {
    setStatus(leaveTypeFilterStatus, `Erreur: ${error.message}`, false);
  }
});

leaveUpdateForm?.addEventListener("submit", async (event) => {
  event.preventDefault();
  if (!state.accessToken) {
    setStatus(leaveUpdateStatus, "Connectez-vous avec un compte autorisé.", false);
    return;
  }

  const data = new FormData(leaveUpdateForm);
  const requestId = Number(data.get("requestId"));
  const payload = {
    status: Number(data.get("status")),
    managerComments: data.get("managerComments") || null,
  };

  setStatus(leaveUpdateStatus, "Mise à jour en cours...");
  try {
    await apiFetch(`/api/LeaveRequests/${requestId}/status`, { method: "PUT", body: payload });
    setStatus(leaveUpdateStatus, "Statut mis à jour.");
    loadLeaveRequests();
  } catch (error) {
    setStatus(leaveUpdateStatus, `Erreur: ${error.message}`, false);
  }
});

leaveRemainingForm?.addEventListener("submit", async (event) => {
  event.preventDefault();
  if (!ensureAuthenticated()) {
    setStatus(leaveRemainingStatus, "Connectez-vous pour consulter le solde.", false);
    return;
  }

  const data = new FormData(leaveRemainingForm);
  const employeeId = Number(data.get("employeeId"));
  const year = Number(data.get("year"));
  setStatus(leaveRemainingStatus, "Calcul en cours...");
  try {
    const remainingDays = await apiFetch(`/api/LeaveRequests/employee/${employeeId}/remaining/${year}`);
    setStatus(leaveRemainingStatus, `${remainingDays} jours restants`);
  } catch (error) {
    setStatus(leaveRemainingStatus, `Erreur: ${error.message}`, false);
  }
});

leaveConflictForm?.addEventListener("submit", async (event) => {
  event.preventDefault();
  if (!ensureAuthenticated()) {
    setStatus(leaveConflictStatus, "Connectez-vous pour vérifier les conflits.", false);
    return;
  }

  const data = new FormData(leaveConflictForm);
  const employeeId = Number(data.get("employeeId"));
  const startDate = new Date(data.get("startDate"));
  const endDate = new Date(data.get("endDate"));
  const params = new URLSearchParams({
    startDate: startDate.toISOString(),
    endDate: endDate.toISOString(),
  });

  setStatus(leaveConflictStatus, "Vérification en cours...");
  try {
    const hasConflict = await apiFetch(`/api/LeaveRequests/employee/${employeeId}/conflicts?${params}`);
    const message = hasConflict ? "Conflit détecté sur cette période." : "Aucun conflit détecté.";
    setStatus(leaveConflictStatus, message, !hasConflict);
  } catch (error) {
    setStatus(leaveConflictStatus, `Erreur: ${error.message}`, false);
  }
});

attendanceStartDateInput?.addEventListener("change", loadAttendances);
attendanceEndDateInput?.addEventListener("change", loadAttendances);

document.querySelector('[data-action="refresh-employees"]')?.addEventListener("click", loadEmployees);
document.querySelector('[data-action="refresh-departments"]')?.addEventListener("click", loadDepartments);
document.querySelector('[data-action="refresh-attendances"]')?.addEventListener("click", loadAttendances);
document.querySelector('[data-action="refresh-leave-requests"]')?.addEventListener("click", loadLeaveRequests);

if (!checkAuthentication()) {
  throw new Error("Redirecting to login...");
}

updateUserLabel();
renderRoleRoutes();
prefillEmployeeIds();
setupRoleBasedVisibility();
setDefaultDateRange();

loadEmployees();
loadDepartments();
loadAttendances();
loadLeaveRequests();
