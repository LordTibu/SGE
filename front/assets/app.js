const defaultBaseUrl = "http://localhost:5000";
const apiBaseInput = document.querySelector("#api-base");
const saveBaseButton = document.querySelector("#save-base");
const form = document.querySelector("#employee-form");
const formStatus = document.querySelector("#form-status");
const authForm = document.querySelector("#auth-form");
const authStatus = document.querySelector("#auth-status");
const currentUser = document.querySelector("#current-user");
const logoutButton = document.querySelector("#logout");

const attendanceDateInput = document.querySelector("#attendance-date");

const employeesTable = document.querySelector("#employees-table tbody");
const departmentsTable = document.querySelector("#departments-table tbody");
const attendancesTable = document.querySelector("#attendances-table tbody");

const state = {
  baseUrl: localStorage.getItem("sge:baseUrl") || defaultBaseUrl,
  accessToken: localStorage.getItem("sge:accessToken") || "",
  refreshToken: localStorage.getItem("sge:refreshToken") || "",
  user: JSON.parse(localStorage.getItem("sge:user") || "null"),
};

apiBaseInput.value = state.baseUrl;

// set default attendance date to today (YYYY-MM-DD)
if (attendanceDateInput) {
  attendanceDateInput.value = new Date().toISOString().split("T")[0];
}

saveBaseButton.addEventListener("click", () => {
  const value = apiBaseInput.value.trim();
  if (!value) return;
  state.baseUrl = value;
  localStorage.setItem("sge:baseUrl", value);
  setStatus(formStatus, "API base URL mise à jour.");
});

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

function setStatus(element, message, success = true) {
  if (!element) return;
  element.textContent = message;
  element.style.color = success ? "#15803d" : "#b91c1c";
}

function updateUserLabel() {
  if (state.user) {
    const roles = state.user.roles?.length ? ` [${state.user.roles.join(", ")}]` : "";
    currentUser.textContent = `Connecté en tant que ${state.user.firstName} ${state.user.lastName}${roles}`;
  } else {
    currentUser.textContent = "Aucun utilisateur connecté.";
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
    const message = typeof payload === "string" ? payload : payload?.message || payload?.error || JSON.stringify(payload);
    throw new Error(message || `Request failed with status ${response.status}`);
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
    setPlaceholder(departmentsTable, `Erreur lors du chargement: ${error.message}`);
  }
}

async function loadAttendances() {
  try {
    setPlaceholder(attendancesTable, "Chargement...");
    if (!ensureAuthenticated(attendancesTable)) return;

    const date = attendanceDateInput?.value || new Date().toISOString().split("T")[0];
    const attendances = await apiFetch(`/api/Attendances/date/${date}`);
    if (!attendances.length) {
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
    setPlaceholder(attendancesTable, `Erreur lors du chargement: ${error.message}`);
  }
}

form.addEventListener("submit", async (event) => {
  event.preventDefault();
  const data = new FormData(form);
  const payload = Object.fromEntries(data.entries());

  payload.salary = Number(payload.salary);
  payload.departmentId = Number(payload.departmentId);

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
    setStatus(formStatus, `Erreur: ${error.message}`, false);
  }
});

authForm.addEventListener("submit", async (event) => {
  event.preventDefault();
  const data = new FormData(authForm);
  const credentials = {
    email: data.get("email")?.toString() ?? "",
    password: data.get("password")?.toString() ?? "",
  };

  try {
    setStatus(authStatus, "Connexion en cours...");
    const authResponse = await apiFetch(`/api/Auth/login`, {
      method: "POST",
      body: credentials,
    });

    state.accessToken = authResponse.accessToken;
    state.refreshToken = authResponse.refreshToken;
    state.user = authResponse.user;
    persistAuth();
    updateUserLabel();
    setStatus(authStatus, "Connecté avec succès.");
    loadEmployees();
    loadDepartments();
    loadAttendances();
  } catch (error) {
    setStatus(authStatus, `Erreur: ${error.message}`, false);
  }
});

logoutButton.addEventListener("click", () => {
  clearAuth();
  setStatus(authStatus, "Déconnecté.");
  setPlaceholder(employeesTable, "Connectez-vous pour consulter ces données.");
  setPlaceholder(departmentsTable, "Connectez-vous pour consulter ces données.");
  setPlaceholder(attendancesTable, "Connectez-vous pour consulter ces données.");
});

attendanceDateInput?.addEventListener("change", loadAttendances);
document.querySelector('[data-action="refresh-employees"]').addEventListener("click", loadEmployees);
document.querySelector('[data-action="refresh-departments"]').addEventListener("click", loadDepartments);
document.querySelector('[data-action="refresh-attendances"]').addEventListener("click", loadAttendances);

// Initial load
updateUserLabel();
loadEmployees();
loadDepartments();
loadAttendances();
