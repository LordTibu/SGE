# 📚 API Endpoints - Système de Gestion des Employés (SGE)

## 🌐 Base URL
```
http://localhost:5000/api
```

---

## 📋 Table des matières
- [🔐 Authentification](#-authentification)
- [🏢 Departments](#-departments)
- [👥 Employees](#-employees)
- [⏰ Attendances](#-attendances)
- [🏖️ Leave Requests](#-leave-requests)
- [📊 Codes de statut HTTP](#-codes-de-statut-http)
- [🔑 Rôles et permissions](#-rôles-et-permissions)

---

## 🔐 Authentification

L'API utilise **JWT (JSON Web Token)** pour l'authentification. Tous les endpoints (sauf `/api/Auth/register`, `/api/Auth/login`, `/api/Auth/refresh`) nécessitent un token JWT valide dans le header `Authorization`.

### Protection par rôles (vue synthétique)

- **Sans authentification** : `/api/Auth/register`, `/api/Auth/login`, `/api/Auth/refresh`
- **Authentifié (tous rôles)** : opérations personnelles (voir ses propres données, pointer, créer une demande de congé, voir ses congés, récupérer son profil, se déconnecter)
- **Manager ou Admin** : gestion globale (lister employés/départements/présences, filtrer les congés, approuver/rejeter, exporter), mises à jour courantes (modifier employé ou département)
- **Admin uniquement** : création/suppression de départements ou d’employés, import Excel d’employés

### Format du header d'authentification
```
Authorization: Bearer {votre-access-token}
```

### Rôles disponibles
- **Admin** : Accès complet à toutes les fonctionnalités
- **Manager** : Peut gérer les employés, départements, approuver les congés
- **User** : Accès limité (voir ses propres données, pointer, créer des demandes de congé)

---

## 🔐 Endpoints d'authentification

### 1. Inscription d'un nouvel utilisateur
```http
POST /api/Auth/register
Content-Type: application/json
```

**🔓 Public** - Aucune authentification requise

**Body (JSON) :**
```json
{
  "firstName": "Jean",
  "lastName": "Dupont",
  "email": "jean.dupont@example.com",
  "userName": "jdupont",
  "password": "Password123!",
  "confirmPassword": "Password123!",
  "employeeId": null
}
```

**Réponse (200 OK) :**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "abc123def456...",
  "expiresAt": "2025-12-01T10:30:00Z",
  "user": {
    "id": "guid-here",
    "userName": "jdupont",
    "email": "jean.dupont@example.com",
    "firstName": "Jean",
    "lastName": "Dupont",
    "roles": ["User"],
    "employeeId": null
  }
}
```

**Règles de mot de passe :**
- Minimum 8 caractères
- Au moins 1 chiffre
- Au moins 1 majuscule
- Au moins 1 minuscule

---

### 2. Connexion
```http
POST /api/Auth/login
Content-Type: application/json
```

**🔓 Public** - Aucune authentification requise

**Body (JSON) :**
```json
{
  "email": "admin@example.com",
  "password": "Admin123!"
}
```

**Compte manager par défaut :**
- Email: `manager@example.com`
- Password: `Manager123!`
- Rôle: `Manager`

**Compte admin par défaut :**
- Email: `admin@example.com`
- Password: `Admin123!`
- Rôle: `Admin`

**Réponse (200 OK) :**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "xyz789abc123...",
  "expiresAt": "2025-12-08T10:30:00Z",
  "user": {
    "id": "guid-here",
    "userName": "admin",
    "email": "admin@example.com",
    "firstName": "Super",
    "lastName": "Admin",
    "roles": ["Admin"],
    "employeeId": null
  }
}
```

---

### 3. Rafraîchir le token (Refresh Token)
```http
POST /api/Auth/refresh
Content-Type: application/json
```

**🔓 Public** - Aucune authentification requise

**Body (JSON) :**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "xyz789abc123..."
}
```

**Réponse (200 OK) :**
```json
{
  "accessToken": "nouveau-token...",
  "refreshToken": "nouveau-refresh-token...",
  "expiresAt": "2025-12-08T10:45:00Z",
  "user": { ... }
}
```

---

### 4. Récupérer les informations de l'utilisateur connecté
```http
GET /api/Auth/me/{userId}
Authorization: Bearer {access-token}
```

**🔒 Authentification requise** - Tous les rôles

**Réponse (200 OK) :**
```json
{
  "id": "guid-here",
  "userName": "admin",
  "email": "admin@example.com",
  "firstName": "Super",
  "lastName": "Admin",
  "roles": ["Admin"],
  "employeeId": null
}
```

---

### 5. Déconnexion
```http
POST /api/Auth/logout/{userId}
Authorization: Bearer {access-token}
```

**🔒 Authentification requise** - Tous les rôles

**Réponse (200 OK) :**
```json
{
  "message": "Déconnexion réussie"
}
```

---

### 6. Révoquer un token
```http
POST /api/Auth/revoke
Authorization: Bearer {access-token}
Content-Type: application/json
```

**🔒 Authentification requise** - Tous les rôles

**Body (JSON) :**
```json
"refresh-token-to-revoke"
```

**Réponse (200 OK) :**
```json
{
  "message": "Token révoqué avec succès"
}
```

---

### 7. Mettre à jour les rôles d'un utilisateur
```http
PUT /api/Auth/users/{userId}/roles
Authorization: Bearer {access-token}
Content-Type: application/json
```

**🔒 Rôle requis :** `Admin`

**Body (JSON) :**
```json
{
  "roles": ["Manager", "User"]
}
```

**Rôles disponibles :**
- `Admin` - Accès complet
- `Manager` - Gestion des employés et départements
- `User` - Accès limité

**Réponse (200 OK) :**
```json
{
  "message": "Rôles mis à jour avec succès"
}
```

**Exemple : Promouvoir un utilisateur en Manager**
```json
{
  "roles": ["Manager"]
}
```

**Exemple : Donner plusieurs rôles**
```json
{
  "roles": ["Manager", "User"]
}
```

**Exemple : Retirer tous les rôles**
```json
{
  "roles": []
}
```

---

### 8. Mettre à jour les informations d'un utilisateur
```http
PUT /api/Auth/users/{userId}
Authorization: Bearer {access-token}
Content-Type: application/json
```

**🔒 Rôle requis :** `Admin`, `Manager`

**Body (JSON) :**
```json
{
  "firstName": "Jean",
  "lastName": "Dupont",
  "email": "jean.dupont@example.com",
  "userName": "jdupont",
  "isActive": true,
  "employeeId": 1
}
```

**Tous les champs sont optionnels** - Seuls les champs fournis seront mis à jour.

**Réponse (200 OK) :**
```json
{
  "id": "guid-here",
  "userName": "jdupont",
  "email": "jean.dupont@example.com",
  "firstName": "Jean",
  "lastName": "Dupont",
  "roles": ["User"],
  "employeeId": 1
}
```

**Exemple : Désactiver un compte utilisateur**
```json
{
  "isActive": false
}
```

**Exemple : Changer l'email**
```json
{
  "email": "nouveau.email@example.com"
}
```

---

### 9. Supprimer un utilisateur
```http
DELETE /api/Auth/users/{userId}
Authorization: Bearer {access-token}
```

**🔒 Rôle requis :** `Admin`

**Réponse (200 OK) :**
```json
{
  "message": "Utilisateur supprimé avec succès"
}
```

**Note :** La suppression révoque automatiquement tous les refresh tokens de l'utilisateur avant de le supprimer.

---

## 🏢 Departments

### 1. Créer un département
```http
POST /api/Departments
Authorization: Bearer {access-token}
Content-Type: application/json
```

**🔒 Rôle requis :** `Admin`

**Body (JSON) :**
```json
{
  "name": "Développement",
  "description": "Équipe de développement logiciel"
}
```

---

### 2. Récupérer tous les départements
```http
GET /api/Departments
Authorization: Bearer {access-token}
```

**🔒 Rôle requis :** `Admin`, `Manager`

---

### 3. Récupérer un département par ID
```http
GET /api/Departments/1
Authorization: Bearer {access-token}
```

**🔒 Authentification requise** - Tous les rôles

---

### 4. Mettre à jour un département
```http
PUT /api/Departments/1
Authorization: Bearer {access-token}
Content-Type: application/json
```

**🔒 Rôle requis :** `Admin`, `Manager`

**Body (JSON) :**
```json
{
  "name": "Développement & Innovation",
  "description": "Équipe de développement et R&D"
}
```

---

### 5. Supprimer un département
```http
DELETE /api/Departments/1
Authorization: Bearer {access-token}
```

**🔒 Rôle requis :** `Admin`

---

## 👥 Employees

### 1. Créer un employé
```http
POST /api/Employees
Authorization: Bearer {access-token}
Content-Type: application/json
```

**🔒 Rôle requis :** `Admin`

**Body (JSON) :**
```json
{
  "firstName": "Jean",
  "lastName": "Dupont",
  "email": "jean.dupont@example.com",
  "phoneNumber": "0601020304",
  "address": "123 rue de Paris, 75001 Paris",
  "position": "Développeur Full Stack",
  "salary": 45000,
  "hireDate": "2024-01-15T00:00:00Z",
  "departmentId": 1,
  "status": "Active"
}
```

---

### 2. Récupérer tous les employés
```http
GET /api/Employees
Authorization: Bearer {access-token}
```

**🔒 Rôle requis :** `Admin`, `Manager`

---

### 3. Récupérer un employé par ID
```http
GET /api/Employees/1
Authorization: Bearer {access-token}
```

**🔒 Authentification requise** - Tous les rôles

---

### 4. Récupérer un employé par email
```http
GET /api/Employees/by-email/{email}
Authorization: Bearer {access-token}
```

**🔒 Authentification requise** - Tous les rôles

---

### 5. Récupérer les employés d'un département
```http
GET /api/Employees/by-department/{departmentId}
Authorization: Bearer {access-token}
```

**🔒 Authentification requise** - Tous les rôles

---

### 6. Mettre à jour un employé
```http
PATCH /api/Employees/1
Authorization: Bearer {access-token}
Content-Type: application/json
```

**🔒 Rôle requis :** `Admin`, `Manager`

**Body (JSON) :**
```json
{
  "firstName": "Jean",
  "lastName": "Dupont",
  "phoneNumber": "0601020304",
  "address": "456 avenue des Champs, 75008 Paris",
  "position": "Senior Developer",
  "salary": 55000,
  "departmentId": 1,
  "status": "Active"
}
```

---

### 7. Supprimer un employé
```http
DELETE /api/Employees/1
Authorization: Bearer {access-token}
```

**🔒 Rôle requis :** `Admin`

---

### 8. Exporter les employés en Excel
```http
GET /api/Employees/export/excel
Authorization: Bearer {access-token}
```

**🔒 Rôle requis :** `Admin`, `Manager`

---

### 9. Importer des employés depuis Excel
```http
POST /api/Employees/import
Authorization: Bearer {access-token}
Content-Type: multipart/form-data
```

**🔒 Rôle requis :** `Admin`

**Body (form-data) :**
```
file: [fichier Excel]
```

---

## ⏰ Attendances

### 1. Pointer l'arrivée (Clock In)
```http
POST /api/Attendances/clock-in
Authorization: Bearer {access-token}
Content-Type: application/json
```

**🔒 Authentification requise** - Tous les rôles

**Body (JSON) :**
```json
{
  "employeeId": 1,
  "dateTime": "2025-11-24T08:30:00Z",
  "notes": "Arrivée normale"
}
```

---

### 2. Pointer le départ (Clock Out)
```http
POST /api/Attendances/clock-out
Authorization: Bearer {access-token}
Content-Type: application/json
```

**🔒 Authentification requise** - Tous les rôles

**Body (JSON) :**
```json
{
  "employeeId": 1,
  "dateTime": "2025-11-24T17:30:00Z",
  "notes": "Départ normal"
}
```

---

### 3. Créer un enregistrement de présence complet
```http
POST /api/Attendances
Authorization: Bearer {access-token}
Content-Type: application/json
```

**🔒 Rôle requis :** `Admin`, `Manager`

**Body (JSON) :**
```json
{
  "employeeId": 1,
  "date": "2025-11-24T00:00:00Z",
  "clockIn": "08:30:00",
  "clockOut": "17:30:00",
  "breakDurationHours": 1.0,
  "notes": "Journée complète"
}
```

---

### 4. Récupérer un enregistrement de présence par ID
```http
GET /api/Attendances/1
Authorization: Bearer {access-token}
```

**🔒 Authentification requise** - Tous les rôles

---

### 5. Récupérer les présences d'un employé
```http
GET /api/Attendances/employee/{employeeId}
Authorization: Bearer {access-token}
```

**🔒 Authentification requise** - Tous les rôles

**Avec filtres de dates (optionnels) :**
```http
GET /api/Attendances/employee/1?startDate=2025-11-01T00:00:00Z&endDate=2025-11-30T23:59:59Z
Authorization: Bearer {access-token}
```

---

### 6. Récupérer les présences d'une date spécifique
```http
GET /api/Attendances/date/{date}
Authorization: Bearer {access-token}
```

**🔒 Rôle requis :** `Admin`, `Manager`

**Exemple :**
```http
GET /api/Attendances/date/2025-11-24T00:00:00Z
```

---

### 7. Récupérer la présence du jour d'un employé
```http
GET /api/Attendances/employee/{employeeId}/today
Authorization: Bearer {access-token}
```

**🔒 Authentification requise** - Tous les rôles

---

### 8. Récupérer les heures travaillées mensuelles
```http
GET /api/Attendances/employee/{employeeId}/hours/{year}/{month}
Authorization: Bearer {access-token}
```

**🔒 Authentification requise** - Tous les rôles

**Exemple :**
```http
GET /api/Attendances/employee/1/hours/2025/11
```

---

## 🏖️ Leave Requests

### 1. Créer une demande de congé
```http
POST /api/LeaveRequests
Authorization: Bearer {access-token}
Content-Type: application/json
```

**🔒 Authentification requise** - Tous les rôles

**Body (JSON) :**
```json
{
  "employeeId": 1,
  "leaveType": 1,
  "startDate": "2025-12-15T00:00:00Z",
  "endDate": "2025-12-20T00:00:00Z",
  "reason": "Vacances de fin d'année"
}
```

**Types de congé (leaveType) :**
- `1` = Annual (Congé annuel)
- `2` = Sick (Congé maladie)
- `3` = Maternity (Congé maternité)
- `4` = Paternity (Congé paternité)
- `5` = Personal (Congé personnel)
- `6` = Unpaid (Congé sans solde)

---

### 2. Récupérer une demande de congé par ID
```http
GET /api/LeaveRequests/{id}
Authorization: Bearer {access-token}
```

**🔒 Authentification requise** - Tous les rôles

---

### 3. Récupérer toutes les demandes d'un employé
```http
GET /api/LeaveRequests/employee/{employeeId}
Authorization: Bearer {access-token}
```

**🔒 Authentification requise** - Tous les rôles

---

### 4. Récupérer les demandes par statut
```http
GET /api/LeaveRequests/status/{status}
Authorization: Bearer {access-token}
```

**🔒 Rôle requis :** `Admin`, `Manager`

**Statuts (status) :**
- `1` = Pending (En attente)
- `2` = Approved (Approuvé)
- `3` = Rejected (Rejeté)
- `4` = Cancelled (Annulé)

---

### 5. Récupérer toutes les demandes en attente
```http
GET /api/LeaveRequests/pending
Authorization: Bearer {access-token}
```

**🔒 Rôle requis :** `Admin`, `Manager`

---

### 6. Approuver/Rejeter une demande
```http
PUT /api/LeaveRequests/{id}/status
Authorization: Bearer {access-token}
Content-Type: application/json
```

**🔒 Rôle requis :** `Admin`, `Manager`

**Body (JSON) - Approuver :**
```json
{
  "status": 2,
  "managerComments": "Approuvé - Bonnes vacances !"
}
```

**Body (JSON) - Rejeter :**
```json
{
  "status": 3,
  "managerComments": "Période trop chargée, veuillez choisir une autre date"
}
```

---

### 7. Calculer les jours de congés restants
```http
GET /api/LeaveRequests/employee/{employeeId}/remaining/{year}
Authorization: Bearer {access-token}
```

**🔒 Authentification requise** - Tous les rôles

**Exemple :**
```http
GET /api/LeaveRequests/employee/1/remaining/2025
```

**Retourne :** Nombre de jours de congés annuels restants (sur 25 jours par défaut)

---

### 8. Vérifier les conflits de dates
```http
GET /api/LeaveRequests/employee/{employeeId}/conflicts?startDate={date}&endDate={date}
Authorization: Bearer {access-token}
```

**🔒 Authentification requise** - Tous les rôles

**Exemple :**
```http
GET /api/LeaveRequests/employee/1/conflicts?startDate=2025-12-15T00:00:00Z&endDate=2025-12-20T00:00:00Z
```

**Paramètres optionnels :**
- `excludeRequestId` : ID de demande à exclure de la vérification (utile lors de modification)

---

## 📊 Codes de statut HTTP

### Succès
- `200 OK` - Requête réussie
- `201 Created` - Ressource créée avec succès
- `204 No Content` - Opération réussie sans contenu de retour

### Erreurs Client
- `400 Bad Request` - Données invalides
- `401 Unauthorized` - Token manquant, invalide ou expiré
- `403 Forbidden` - Accès refusé (rôle insuffisant)
- `404 Not Found` - Ressource non trouvée
- `409 Conflict` - Conflit (ex: email déjà existant, congés qui se chevauchent)

### Erreurs Serveur
- `500 Internal Server Error` - Erreur serveur

---

## 🔑 Rôles et permissions

### Matrice des permissions

| Action | User | Manager | Admin |
|--------|------|---------|-------|
| **Authentification** |
| S'inscrire | ✅ | ✅ | ✅ |
| Se connecter | ✅ | ✅ | ✅ |
| Rafraîchir token | ✅ | ✅ | ✅ |
| Voir son profil | ✅ | ✅ | ✅ |
| Modifier son profil | ✅ | ✅ | ✅ |
| Modifier les rôles d'un utilisateur | ❌ | ❌ | ✅ |
| Modifier les infos d'un utilisateur | ❌ | ✅ | ✅ |
| Supprimer un utilisateur | ❌ | ❌ | ✅ |
| **Departments** |
| Voir tous les départements | ❌ | ✅ | ✅ |
| Voir un département | ✅ | ✅ | ✅ |
| Créer un département | ❌ | ❌ | ✅ |
| Modifier un département | ❌ | ✅ | ✅ |
| Supprimer un département | ❌ | ❌ | ✅ |
| **Employees** |
| Voir tous les employés | ❌ | ✅ | ✅ |
| Voir un employé | ✅ | ✅ | ✅ |
| Créer un employé | ❌ | ❌ | ✅ |
| Modifier un employé | ❌ | ✅ | ✅ |
| Supprimer un employé | ❌ | ❌ | ✅ |
| Exporter/Importer | ❌ | ✅ (Export) | ✅ |
| **Attendances** |
| Pointer (Clock-in/out) | ✅ | ✅ | ✅ |
| Voir ses présences | ✅ | ✅ | ✅ |
| Créer manuellement | ❌ | ✅ | ✅ |
| Voir toutes les présences | ❌ | ✅ | ✅ |
| Voir par date | ❌ | ✅ | ✅ |
| **Leave Requests** |
| Créer une demande | ✅ | ✅ | ✅ |
| Voir ses demandes | ✅ | ✅ | ✅ |
| Voir toutes les demandes | ❌ | ✅ | ✅ |
| Voir demandes en attente | ❌ | ✅ | ✅ |
| Approuver/Rejeter | ❌ | ✅ | ✅ |
| Voir jours restants | ✅ | ✅ | ✅ |

---

### Détail par route et rôle requis

#### AuthController
| Route | Méthode | Rôle requis |
|-------|---------|-------------|
| `/api/Auth/register` | POST | Public |
| `/api/Auth/login` | POST | Public |
| `/api/Auth/refresh` | POST | Public |
| `/api/Auth/me/{userId}` | GET | Authentifié (User/Manager/Admin) |
| `/api/Auth/logout/{userId}` | POST | Authentifié (User/Manager/Admin) |
| `/api/Auth/revoke` | POST | Authentifié (User/Manager/Admin) |
| `/api/Auth/users/{userId}/roles` | PUT | Admin |
| `/api/Auth/users/{userId}` | PUT | Admin ou Manager |
| `/api/Auth/users/{userId}` | DELETE | Admin |

#### DepartmentsController (`/api/Departments`)
| Route | Méthode | Rôle requis |
|-------|---------|-------------|
| `/` | GET | Manager ou Admin |
| `/{id}` | GET | Authentifié (User/Manager/Admin) |
| `/` | POST | Admin |
| `/{id}` | PATCH | Manager ou Admin |
| `/{id}` | DELETE | Admin |

#### EmployeesController (`/api/Employees`)
| Route | Méthode | Rôle requis |
|-------|---------|-------------|
| `/` | GET | Manager ou Admin |
| `/{id}` | GET | Authentifié (User/Manager/Admin) |
| `/by-email/{email}` | GET | Authentifié (User/Manager/Admin) |
| `/by-department/{departmentId}` | GET | Authentifié (User/Manager/Admin) |
| `/` | POST | Admin |
| `/{id}` | PATCH | Manager ou Admin |
| `/{id}` | DELETE | Admin |
| `/export/excel` | GET | Manager ou Admin |
| `/import` | POST | Admin |

#### AttendancesController (`/api/Attendances`)
| Route | Méthode | Rôle requis |
|-------|---------|-------------|
| `/clock-in` | POST | Authentifié (User/Manager/Admin) |
| `/clock-out` | POST | Authentifié (User/Manager/Admin) |
| `/` | POST | Manager ou Admin |
| `/{id}` | GET | Authentifié (User/Manager/Admin) |
| `/employee/{employeeId}` | GET | Authentifié (User/Manager/Admin) |
| `/date/{date}` | GET | Manager ou Admin |
| `/employee/{employeeId}/today` | GET | Authentifié (User/Manager/Admin) |
| `/employee/{employeeId}/hours/{year}/{month}` | GET | Authentifié (User/Manager/Admin) |

#### LeaveRequestsController (`/api/LeaveRequests`)
| Route | Méthode | Rôle requis |
|-------|---------|-------------|
| `/` | POST | Authentifié (User/Manager/Admin) |
| `/{id}` | GET | Authentifié (User/Manager/Admin) |
| `/employee/{employeeId}` | GET | Authentifié (User/Manager/Admin) |
| `/status/{status}` | GET | Manager ou Admin |
| `/pending` | GET | Manager ou Admin |
| `/{id}/status` | PUT | Manager ou Admin |
| `/employee/{employeeId}/remaining/{year}` | GET | Authentifié (User/Manager/Admin) |
| `/employee/{employeeId}/conflicts` | GET | Authentifié (User/Manager/Admin) |

---

## 🧪 Scénarios de test complets

### Scénario 1 : Authentification et création d'employé

**1. Se connecter en tant qu'admin**
```http
POST /api/Auth/login
{
  "email": "admin@example.com",
  "password": "Admin123!"
}
```
→ Récupérer le `accessToken` de la réponse

**2. Créer un département**
```http
POST /api/Departments
Authorization: Bearer {access-token}
{
  "name": "IT",
  "description": "Département IT"
}
```

**3. Créer un employé**
```http
POST /api/Employees
Authorization: Bearer {access-token}
{
  "firstName": "Marie",
  "lastName": "Martin",
  "email": "marie.martin@test.com",
  "phoneNumber": "0612345678",
  "address": "10 rue Test",
  "position": "Développeuse",
  "salary": 42000,
  "hireDate": "2025-01-01T00:00:00Z",
  "departmentId": 1
}
```

---

### Scénario 2 : Pointage et présence

**1. Pointer l'arrivée**
```http
POST /api/Attendances/clock-in
Authorization: Bearer {access-token}
{
  "employeeId": 1,
  "dateTime": "2025-11-24T09:00:00Z"
}
```

**2. Pointer le départ**
```http
POST /api/Attendances/clock-out
Authorization: Bearer {access-token}
{
  "employeeId": 1,
  "dateTime": "2025-11-24T18:00:00Z"
}
```

**3. Voir les heures mensuelles**
```http
GET /api/Attendances/employee/1/hours/2025/11
Authorization: Bearer {access-token}
```

---

### Scénario 3 : Demande de congé complète

**1. Créer une demande (en tant qu'employé)**
```http
POST /api/LeaveRequests
Authorization: Bearer {access-token}
{
  "employeeId": 1,
  "leaveType": 1,
  "startDate": "2025-12-20T00:00:00Z",
  "endDate": "2025-12-31T00:00:00Z",
  "reason": "Vacances de Noël"
}
```

**2. Voir les demandes en attente (en tant que Manager/Admin)**
```http
GET /api/LeaveRequests/pending
Authorization: Bearer {access-token-manager}
```

**3. Approuver la demande (en tant que Manager/Admin)**
```http
PUT /api/LeaveRequests/1/status
Authorization: Bearer {access-token-manager}
{
  "status": 2,
  "managerComments": "Approuvé"
}
```

**4. Vérifier les jours restants**
```http
GET /api/LeaveRequests/employee/1/remaining/2025
Authorization: Bearer {access-token}
```

---

### Scénario 4 : Test des permissions

**1. Tenter d'accéder sans token**
```http
GET /api/Employees
```
**Réponse attendue :** `401 Unauthorized`

**2. Tenter avec token User (rôle insuffisant)**
```http
GET /api/Employees
Authorization: Bearer {access-token-user}
```
**Réponse attendue :** `403 Forbidden`

**3. Accéder avec token Admin**
```http
GET /api/Employees
Authorization: Bearer {access-token-admin}
```
**Réponse attendue :** `200 OK` avec la liste des employés

---

## 🔑 Règles métier importantes

### Authentification
- ✅ Les tokens JWT expirent après 15 minutes (configurable)
- ✅ Les refresh tokens expirent après 7 jours (configurable)
- ✅ Un utilisateur peut avoir plusieurs refresh tokens actifs
- ✅ La déconnexion révoque tous les refresh tokens de l'utilisateur

### Attendances
- ✅ Un employé ne peut avoir qu'un seul enregistrement de présence par jour
- ✅ Le Clock Out nécessite un Clock In préalable
- ✅ Les heures supplémentaires sont calculées au-delà de 8h/jour
- ✅ Les pauses sont déduites des heures travaillées

### Leave Requests
- ✅ Les demandes ne peuvent pas être créées pour des dates passées
- ✅ La date de fin doit être >= à la date de début
- ✅ Les congés qui se chevauchent sont détectés automatiquement
- ✅ Seules les demandes "Pending" peuvent être modifiées
- ✅ Les jours ouvrables sont calculés automatiquement (exclut samedi/dimanche)
- ✅ 25 jours de congés annuels par défaut par employé

---

## 📝 Notes importantes

- **Toutes les dates** doivent être au format ISO 8601 : `YYYY-MM-DDTHH:mm:ssZ`
- **Les heures** sont en format `HH:mm:ss` pour les endpoints d'attendance
- **Le fuseau horaire** est UTC
- **Les réponses** sont en JSON
- **Le token JWT** doit être inclus dans le header `Authorization: Bearer {token}` pour tous les endpoints protégés
- **Les endpoints publics** sont uniquement : `/api/Auth/register`, `/api/Auth/login`, `/api/Auth/refresh`

---

## 🚀 Démarrage rapide

### Avec Docker
```bash
docker compose up -d
```

### L'API sera disponible sur
```
http://localhost:5000
```

### Le front seras accèsible sur
```
http://localhost:4173
```

### Compte administrateur par défaut
- **Email :** `admin@example.com`
- **Password :** `Admin123!`
- **Rôle :** `Admin`

---

## 💡 Utilisation dans Postman

### Configuration des variables
1. Créer une collection "SGE API"
2. Ajouter des variables :
   - `baseUrl` = `http://localhost:5000`
   - `accessToken` = (vide au début)
   - `refreshToken` = (vide au début)

### Script Post-Test pour sauvegarder automatiquement les tokens
```javascript
// Dans le test de /api/Auth/login
if (pm.response.code === 200) {
    var jsonData = pm.response.json();
    pm.collectionVariables.set("accessToken", jsonData.accessToken);
    pm.collectionVariables.set("refreshToken", jsonData.refreshToken);
}
```

### Utilisation dans les requêtes
- URL : `{{baseUrl}}/api/Employees`
- Header : `Authorization: Bearer {{accessToken}}`

---

## 🔍 Visualiser le contenu d'un JWT

Pour décoder et voir le contenu d'un JWT :
1. Copier le `accessToken` de la réponse
2. Aller sur https://jwt.io/
3. Coller le token dans la section "Encoded"
4. Vous verrez le payload avec les claims (id, email, roles, etc.)

---

**Dernière mise à jour :** 24 novembre 2025
