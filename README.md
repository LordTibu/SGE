# 📚 API Endpoints - Système de Gestion des Employés (SGE)

## 🌐 Base URL
```
http://localhost:5000/api
```

---

## 📋 Table des matières
- [Departments](#-departments)
- [Employees](#-employees)
- [Attendances](#-attendances)
- [Leave Requests](#-leave-requests)

---

## 🏢 Departments

### 1. Créer un département
```http
POST /api/Departments
Content-Type: application/json

{
  "name": "Développement",
  "description": "Équipe de développement logiciel"
}
```

### 2. Récupérer tous les départements
```http
GET /api/Departments
```

### 3. Récupérer un département par ID
```http
GET /api/Departments/1
```

### 4. Mettre à jour un département
```http
PUT /api/Departments/1
Content-Type: application/json

{
  "name": "Développement & Innovation",
  "description": "Équipe de développement et R&D"
}
```

### 5. Supprimer un département
```http
DELETE /api/Departments/1
```

### 6. Récupérer les employés d'un département
```http
GET /api/Departments/1/employees
```

---

## 👥 Employees

### 1. Créer un employé
```http
POST /api/Employees
Content-Type: application/json

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

### 2. Récupérer tous les employés
```http
GET /api/Employees
```

### 3. Récupérer un employé par ID
```http
GET /api/Employees/1
```

### 4. Mettre à jour un employé
```http
PUT /api/Employees/1
Content-Type: application/json

{
  "firstName": "Jean",
  "lastName": "Dupont",
  "email": "jean.dupont@example.com",
  "phoneNumber": "0601020304",
  "address": "456 avenue des Champs, 75008 Paris",
  "position": "Senior Developer",
  "salary": 55000,
  "departmentId": 1,
  "status": "Active"
}
```

### 5. Supprimer un employé
```http
DELETE /api/Employees/1
```

### 6. Rechercher des employés
```http
GET /api/Employees/search?searchTerm=jean
```

### 7. Récupérer les employés par statut
```http
GET /api/Employees/status/Active
```
**Statuts disponibles:** `Active`, `Inactive`, `OnLeave`

### 8. Importer des employés (CSV)
```http
POST /api/Employees/import
Content-Type: multipart/form-data

file: [fichier CSV]
```

**Format CSV attendu:**
```csv
FirstName,LastName,Email,PhoneNumber,Address,Position,Salary,HireDate,DepartmentId
Jean,Dupont,jean.dupont@test.com,0601020304,123 rue Test,Développeur,45000,2024-01-15,1
```

---

## ⏰ Attendances

### 1. Pointer l'arrivée (Clock In)
```http
POST /api/Attendances/clock-in
Content-Type: application/json

{
  "employeeId": 1,
  "dateTime": "2025-11-24T08:30:00Z",
  "notes": "Arrivée normale"
}
```

### 2. Pointer le départ (Clock Out)
```http
POST /api/Attendances/clock-out
Content-Type: application/json

{
  "employeeId": 1,
  "dateTime": "2025-11-24T17:30:00Z",
  "notes": "Départ normal"
}
```

### 3. Créer un enregistrement de présence complet
```http
POST /api/Attendances
Content-Type: application/json

{
  "employeeId": 1,
  "date": "2025-11-24T00:00:00Z",
  "clockIn": "08:30:00",
  "clockOut": "17:30:00",
  "breakDurationHours": 1.0,
  "notes": "Journée complète"
}
```

### 4. Récupérer un enregistrement de présence par ID
```http
GET /api/Attendances/1
```

### 5. Récupérer les présences d'un employé
```http
GET /api/Attendances/employee/1
```

**Avec filtres de dates:**
```http
GET /api/Attendances/employee/1?startDate=2025-11-01T00:00:00Z&endDate=2025-11-30T23:59:59Z
```

### 6. Récupérer les présences d'une date spécifique
```http
GET /api/Attendances/date/2025-11-24T00:00:00Z
```

### 7. Récupérer la présence du jour d'un employé
```http
GET /api/Attendances/employee/1/today
```

### 8. Récupérer les heures travaillées mensuelles
```http
GET /api/Attendances/employee/1/hours/2025/11
```
Format: `/employee/{employeeId}/hours/{year}/{month}`

---

## 🏖️ Leave Requests

### 1. Créer une demande de congé
```http
POST /api/LeaveRequests
Content-Type: application/json

{
  "employeeId": 1,
  "leaveType": 1,
  "startDate": "2025-12-15T00:00:00Z",
  "endDate": "2025-12-20T00:00:00Z",
  "reason": "Vacances de fin d'année"
}
```

**Types de congé (leaveType):**
- `1` = Annual (Congé annuel)
- `2` = Sick (Congé maladie)
- `3` = Maternity (Congé maternité)
- `4` = Paternity (Congé paternité)
- `5` = Personal (Congé personnel)
- `6` = Unpaid (Congé sans solde)

### 2. Récupérer une demande de congé par ID
```http
GET /api/LeaveRequests/1
```

### 3. Récupérer toutes les demandes d'un employé
```http
GET /api/LeaveRequests/employee/1
```

### 4. Récupérer les demandes par statut
```http
GET /api/LeaveRequests/status/1
```

**Statuts (status):**
- `1` = Pending (En attente)
- `2` = Approved (Approuvé)
- `3` = Rejected (Rejeté)
- `4` = Cancelled (Annulé)

### 5. Récupérer toutes les demandes en attente
```http
GET /api/LeaveRequests/pending
```

### 6. Approuver/Rejeter une demande
```http
PUT /api/LeaveRequests/1/status
Content-Type: application/json

{
  "status": 2,
  "managerComments": "Approuvé - Bonnes vacances !"
}
```

**Exemples de mises à jour:**

**Approuver:**
```json
{
  "status": 2,
  "managerComments": "Demande approuvée"
}
```

**Rejeter:**
```json
{
  "status": 3,
  "managerComments": "Période trop chargée, veuillez choisir une autre date"
}
```

### 7. Calculer les jours de congés restants
```http
GET /api/LeaveRequests/employee/1/remaining/2025
```
Format: `/employee/{employeeId}/remaining/{year}`

**Retourne:** Nombre de jours de congés annuels restants (sur 25 jours par défaut)

### 8. Vérifier les conflits de dates
```http
GET /api/LeaveRequests/employee/1/conflicts?startDate=2025-12-15T00:00:00Z&endDate=2025-12-20T00:00:00Z
```

**Paramètres optionnels:**
- `excludeRequestId` : ID de demande à exclure de la vérification (utile lors de modification)

**Exemple avec exclusion:**
```http
GET /api/LeaveRequests/employee/1/conflicts?startDate=2025-12-15T00:00:00Z&endDate=2025-12-20T00:00:00Z&excludeRequestId=5
```

---

## 📊 Codes de statut HTTP

### Succès
- `200 OK` - Requête réussie
- `201 Created` - Ressource créée avec succès
- `204 No Content` - Opération réussie sans contenu de retour

### Erreurs Client
- `400 Bad Request` - Données invalides
- `404 Not Found` - Ressource non trouvée
- `409 Conflict` - Conflit (ex: email déjà existant, congés qui se chevauchent)

### Erreurs Serveur
- `500 Internal Server Error` - Erreur serveur

---

## 🧪 Scénarios de test complets

### Scénario 1 : Nouveau employé et première présence

**1. Créer un département**
```http
POST /api/Departments
{ "name": "IT", "description": "Département IT" }
```

**2. Créer un employé**
```http
POST /api/Employees
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

**3. Pointer l'arrivée**
```http
POST /api/Attendances/clock-in
{
  "employeeId": 1,
  "dateTime": "2025-11-24T09:00:00Z"
}
```

**4. Pointer le départ**
```http
POST /api/Attendances/clock-out
{
  "employeeId": 1,
  "dateTime": "2025-11-24T18:00:00Z"
}
```

### Scénario 2 : Demande de congé complète

**1. Créer une demande**
```http
POST /api/LeaveRequests
{
  "employeeId": 1,
  "leaveType": 1,
  "startDate": "2025-12-20T00:00:00Z",
  "endDate": "2025-12-31T00:00:00Z",
  "reason": "Vacances de Noël"
}
```

**2. Vérifier les demandes en attente**
```http
GET /api/LeaveRequests/pending
```

**3. Approuver la demande**
```http
PUT /api/LeaveRequests/1/status
{
  "status": 2,
  "managerComments": "Approuvé"
}
```

**4. Vérifier les jours restants**
```http
GET /api/LeaveRequests/employee/1/remaining/2025
```

### Scénario 3 : Gestion des conflits

**1. Créer une première demande**
```http
POST /api/LeaveRequests
{
  "employeeId": 1,
  "leaveType": 1,
  "startDate": "2025-12-15T00:00:00Z",
  "endDate": "2025-12-20T00:00:00Z",
  "reason": "Vacances"
}
```

**2. Approuver la demande**
```http
PUT /api/LeaveRequests/1/status
{ "status": 2, "managerComments": "OK" }
```

**3. Tenter de créer une demande qui chevauche (devrait échouer)**
```http
POST /api/LeaveRequests
{
  "employeeId": 1,
  "leaveType": 2,
  "startDate": "2025-12-18T00:00:00Z",
  "endDate": "2025-12-22T00:00:00Z",
  "reason": "Maladie"
}
```
**Résultat attendu:** Erreur 409 - "There is already a leave request for this period"

---

## 🔑 Règles métier importantes

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

## 📝 Notes

- Toutes les dates doivent être au format ISO 8601 : `YYYY-MM-DDTHH:mm:ssZ`
- Les heures sont en format `HH:mm:ss` pour les endpoints d'attendance
- Le fuseau horaire est UTC
- Les réponses sont en JSON
- L'authentification n'est pas encore implémentée (TODO)

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

**Dernière mise à jour:** 24 novembre 2025

