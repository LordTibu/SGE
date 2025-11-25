const defaultBaseUrl = "http://localhost:5222";

const apiBaseLoginInput = document.querySelector("#api-base-login");
const loginForm = document.querySelector("#login-form");
const loginStatus = document.querySelector("#login-status");

apiBaseLoginInput.value = localStorage.getItem("sge:baseUrl") || defaultBaseUrl;

function setStatus(element, message, success = true) {
  if (!element) return;
  element.textContent = message;
  element.className = `status-message ${success ? "success" : "error"}`;
  element.style.display = "block";
}

async function apiFetch(baseUrl, path, options = {}) {
  const url = path.startsWith("http") ? path : `${baseUrl}${path}`;
  const headers = new Headers(options.headers || {});

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
    throw error;
  }

  return payload;
}

loginForm.addEventListener("submit", async (event) => {
  event.preventDefault();

  const baseUrl = apiBaseLoginInput.value.trim() || defaultBaseUrl;
  const data = new FormData(loginForm);
  const credentials = {
    email: data.get("email")?.toString() ?? "",
    password: data.get("password")?.toString() ?? "",
  };

  try {
    setStatus(loginStatus, "Connexion en cours...", true);

    const authResponse = await apiFetch(baseUrl, `/api/Auth/login`, {
      method: "POST",
      body: credentials,
    });

    // Store auth data
    localStorage.setItem("sge:baseUrl", baseUrl);
    localStorage.setItem("sge:accessToken", authResponse.accessToken);
    localStorage.setItem("sge:refreshToken", authResponse.refreshToken);
    localStorage.setItem("sge:user", JSON.stringify(authResponse.user));

    // Redirect to dashboard
    setStatus(loginStatus, "Connecté avec succès", true);
    setTimeout(() => {
      window.location.href = "./index.html";
    }, 1000);
  } catch (error) {
    console.error("Login error:", error);
    let errorMessage = error.message;
    
    if (error.status === 401) {
      errorMessage = "Email ou mot de passe incorrect.";
    } else if (error.status === 400) {
      errorMessage = "Données invalides. Vérifiez votre saisie.";
    } else if (!navigator.onLine) {
      errorMessage = "Erreur réseau. Vérifiez votre connexion.";
    }
    
    setStatus(loginStatus, `Erreur: ${errorMessage}`, false);
  }
});
