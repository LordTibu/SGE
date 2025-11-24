const defaultBaseUrl = "http://localhost:5000";
const apiBaseInput = document.querySelector("#api-base");
const saveBaseButton = document.querySelector("#save-base");
const form = document.querySelector("#employee-form");
const formStatus = document.querySelector("#form-status");

const attendanceDateInput = document.querySelector("#attendance-date");

const employeesTable = document.querySelector("#employees-table tbody");
const departmentsTable = document.querySelector("#departments-table tbody");
const attendancesTable = document.querySelector("#attendances-table tbody");

const state = {
  baseUrl: localStorage.getItem("sge:baseUrl") || defaultBaseUrl,
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
  toast("API base URL updated.");
});

async function fetchJson(path) {
  const response = await fetch(path);
  if (!response.ok) {
    const message = await response.text();
    throw new Error(message || `Request failed with status ${response.status}`);
  }
  return response.json();
}

function setPlaceholder(tableEl, message) {
  tableEl.innerHTML = `<tr class="placeholder"><td colspan="${tableEl.parentElement.querySelectorAll("th").length}">${message}</td></tr>`;
}

async function loadEmployees() {
  try {
    setPlaceholder(employeesTable, "Loading...");
    const employees = await fetchJson(`${state.baseUrl}/api/Employees`);
    if (!employees.length) {
      setPlaceholder(employeesTable, "No employees found.");
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
    setPlaceholder(employeesTable, `Failed to load employees: ${error.message}`);
  }
}

async function loadDepartments() {
  try {
    setPlaceholder(departmentsTable, "Loading...");
    const departments = await fetchJson(`${state.baseUrl}/api/Departments`);
    if (!departments.length) {
      setPlaceholder(departmentsTable, "No departments found.");
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
    setPlaceholder(departmentsTable, `Failed to load departments: ${error.message}`);
  }
}

async function loadAttendances() {
  try {
    setPlaceholder(attendancesTable, "Loading...");
    const date = attendanceDateInput?.value || new Date().toISOString().split("T")[0];
    const attendances = await fetchJson(`${state.baseUrl}/api/Attendances/date/${date}`);
    if (!attendances.length) {
      setPlaceholder(attendancesTable, "No attendances found.");
      return;
    }

    attendancesTable.innerHTML = attendances
      .map(
        (att) => `
          <tr>
            <td>${att.id}</td>
            <td>${att.employeeId}</td>
            <td>${att.date?.split("T")[0] ?? ""}</td>
            <td>${att.clockIn} "/" ${att.clockOut}</td>
            <td>${att.notes ?? ""}</td>
          </tr>
        `,
      )
      .join("");
  } catch (error) {
    setPlaceholder(attendancesTable, `Failed to load attendances: ${error.message}`);
  }
}

function toast(message, success = true) {
  formStatus.textContent = message;
  formStatus.style.color = success ? "#15803d" : "#b91c1c";
}

form.addEventListener("submit", async (event) => {
  event.preventDefault();
  const data = new FormData(form);
  const payload = Object.fromEntries(data.entries());

  payload.salary = Number(payload.salary);
  payload.departmentId = Number(payload.departmentId);

  try {
    formStatus.textContent = "Submitting...";
    const response = await fetch(`${state.baseUrl}/api/Employees`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(payload),
    });

    if (!response.ok) {
      const message = await response.text();
      throw new Error(message || `Request failed with status ${response.status}`);
    }

    toast("Employee created successfully.");
    form.reset();
    loadEmployees();
  } catch (error) {
    toast(`Error: ${error.message}`, false);
  }
});

document.querySelector('[data-action="refresh-employees"]').addEventListener("click", loadEmployees);
document.querySelector('[data-action="refresh-departments"]').addEventListener("click", loadDepartments);
document.querySelector('[data-action="refresh-attendances"]').addEventListener("click", loadAttendances);

// Initial load
loadEmployees();
loadDepartments();
loadAttendances();
