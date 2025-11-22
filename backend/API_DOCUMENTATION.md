# API Documentation

Base URL: `http://localhost:5000`

## Authentication Endpoints

### POST /auth/login
Login with email and password.

**Request Body:**
```json
{
  "email": "ali@nb.com",
  "password": "password"
}
```

**Response (200 OK):**
```json
{
  "userId": 1,
  "name": "Ali (Bank Admin)",
  "role": "Admin",
  "tenantId": 1,
  "tenantName": "National Bank of Iraq"
}
```

**Response (401 Unauthorized):**
Invalid credentials

---

### POST /auth/register
Register a new user with unique organization.

**Request Body:**
```json
{
  "name": "John Doe",
  "email": "john@example.com",
  "password": "yourpassword"
}
```

**Response (200 OK):**
```json
{
  "userId": 4,
  "name": "John Doe",
  "role": "Editor",
  "tenantId": 4,
  "tenantName": "John Doe's Organization"
}
```

**Response (409 Conflict):**
```
Email already registered.
```

---

## Form Endpoints

### GET /forms
Get all forms for the current tenant.

**Query Parameters:**
- `tenantId` (required): The tenant ID

**Response (200 OK):**
```json
[
  {
    "id": 1,
    "title": "Hackathon Feedback Survey",
    "description": "Tell us about your experience!",
    "isPublished": true,
    "isPublic": true,
    "startDate": "2025-01-01T00:00:00Z",
    "endDate": "2025-12-31T23:59:59Z",
    "oneSubmissionPerUser": false,
    "version": 1,
    "parentGroupId": null,
    "tenantId": 1,
    "questions": [
      {
        "id": 1,
        "formId": 1,
        "label": "How was the food?",
        "helpText": "",
        "type": "Rating",
        "isRequired": false,
        "placeholder": "",
        "defaultValue": "",
        "validationRules": "",
        "options": "1-5"
      }
    ]
  }
]
```

---

### GET /forms/{id}
Get a specific form by ID with all questions.

**Path Parameters:**
- `id` (required): Form ID

**Response (200 OK):**
```json
{
  "id": 1,
  "title": "Hackathon Feedback Survey",
  "description": "Tell us about your experience!",
  "isPublished": true,
  "isPublic": true,
  "startDate": "2025-01-01T00:00:00Z",
  "endDate": "2025-12-31T23:59:59Z",
  "oneSubmissionPerUser": false,
  "version": 1,
  "parentGroupId": null,
  "tenantId": 1,
  "questions": [...]
}
```

**Response (404 Not Found):**
Form not found

---

### POST /forms
Create a new form.

**Request Body:**
```json
{
  "title": "Employee Survey",
  "description": "Annual employee feedback",
  "isPublished": true,
  "isPublic": false,
  "startDate": "2025-01-01T00:00:00Z",
  "endDate": "2025-12-31T23:59:59Z",
  "oneSubmissionPerUser": true,
  "tenantId": 1,
  "questions": [
    {
      "label": "How satisfied are you?",
      "helpText": "Rate from 1 to 5",
      "type": "Rating",
      "isRequired": true,
      "placeholder": "",
      "defaultValue": "",
      "validationRules": "",
      "options": "1-5"
    },
    {
      "label": "Which department?",
      "helpText": "",
      "type": "Dropdown",
      "isRequired": true,
      "placeholder": "Select...",
      "defaultValue": "",
      "validationRules": "",
      "options": "HR,IT,Sales,Marketing"
    },
    {
      "label": "Additional comments",
      "helpText": "",
      "type": "Text",
      "isRequired": false,
      "placeholder": "Type here...",
      "defaultValue": "",
      "validationRules": "",
      "options": null
    }
  ]
}
```

**Response (200 OK):**
Returns the created form with ID and all questions

---

### PUT /forms/{id}
Update an existing form.
- If form has submissions: Creates a new version (new ID)
- If form has no submissions: Updates in place (same ID)

**Path Parameters:**
- `id` (required): Form ID to update

**Request Body:**
Same structure as POST /forms

**Response (200 OK):**
Returns the updated or new form with all questions

**Response (404 Not Found):**
Form not found

---

### DELETE /forms/{id}
Delete a form and all its questions (cascade delete).

**Path Parameters:**
- `id` (required): Form ID

**Response (204 No Content):**
Form deleted successfully

**Response (404 Not Found):**
Form not found

---

### POST /forms/{id}/submit
Submit a response to a form.

**Path Parameters:**
- `id` (required): Form ID

**Request Body:**
```json
{
  "userId": 1,
  "answers": [
    {
      "questionId": 1,
      "value": "5"
    },
    {
      "questionId": 2,
      "value": "Backend"
    }
  ]
}
```

**Response (200 OK):**
```json
{
  "submissionId": 1,
  "message": "Form submitted successfully"
}
```

**Response (404 Not Found):**
Form not found

---

### GET /forms/{id}/stats
Get statistics for form submissions.

**Path Parameters:**
- `id` (required): Form ID

**Response (200 OK):**
```json
{
  "formId": 1,
  "totalSubmissions": 15,
  "questions": [...],
  "analytics": [
    {
      "questionId": 1,
      "answer": "5",
      "count": 8
    },
    {
      "questionId": 1,
      "answer": "4",
      "count": 5
    },
    {
      "questionId": 2,
      "answer": "Backend",
      "count": 10
    }
  ]
}
```

**Response (404 Not Found):**
Form not found

---

## Tenant Endpoints

### GET /tenants
Get all tenants/organizations.

**Response (200 OK):**
```json
[
  {
    "id": 1,
    "name": "National Bank of Iraq",
    "logoUrl": ""
  },
  {
    "id": 2,
    "name": "Zain",
    "logoUrl": ""
  }
]
```

---

## User Endpoints

### GET /users
Get all users.

**Response (200 OK):**
```json
[
  {
    "id": 1,
    "name": "Ali (Bank Admin)",
    "email": "ali@nb.com",
    "password": "password",
    "phoneNumber": "",
    "age": 0,
    "role": "Admin",
    "tenantId": 1
  }
]
```

---

## Question Types

The API supports three question types:

1. **Text**: Free-form text input
   - `type`: "Text"
   - `options`: null

2. **Rating**: Numeric rating
   - `type`: "Rating"
   - `options`: "1-5" (or any range like "1-10")

3. **Dropdown**: Multiple choice selection
   - `type`: "Dropdown"
   - `options`: "Option1,Option2,Option3" (comma-separated)

---

## CORS

CORS is enabled for all origins (`AllowAll` policy).

---

## Demo Accounts

```
Email: ali@nb.com
Password: password
Organization: National Bank of Iraq (Tenant ID: 1)

Email: sarah@zain.com
Password: password
Organization: Zain (Tenant ID: 2)

Email: ahmed@cbi.com
Password: password
Organization: CBI (Tenant ID: 3)
```

---

## Notes

- **Dates**: Use ISO 8601 format: `"2025-11-22T10:30:00Z"`. In JavaScript, use `new Date(dateString).toISOString()` to convert any date to the correct format.
- Each new user registration creates a unique tenant/organization
- Form versioning happens automatically when editing forms with submissions
- Deleting a form cascades to all questions, submissions, and answers
