/**
 * SG71 User Management API — browser / Node.js client
 *
 * Usage (website backend — never expose apiKey in public frontend JS):
 *
 *   import { SG71UserApi } from './sg71-user-api.js'
 *
 *   const api = new SG71UserApi({
 *     apiBase: 'https://sg71auth.netlify.app/api',
 *     adminId: 'your-app-id',
 *     appName: 'your-app-name',
 *     apiKey: process.env.SG71_API_KEY
 *   })
 *
 *   const { users } = await api.listUsers()
 */

export class SG71UserApi {
  constructor({ apiBase, adminId, appName, apiKey }) {
    if (!apiBase || !adminId || !appName || !apiKey) {
      throw new Error('apiBase, adminId, appName, and apiKey are required')
    }
    this.apiBase = apiBase.replace(/\/$/, '')
    this.adminId = adminId
    this.appName = appName
    this.apiKey = apiKey
  }

  async post(path, payload = {}) {
    const response = await fetch(`${this.apiBase}${path}`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-Api-Key': this.apiKey
      },
      body: JSON.stringify({
        adminId: this.adminId,
        appName: this.appName,
        apiKey: this.apiKey,
        ...payload
      })
    })

    const raw = await response.text()
    let data
    try {
      data = raw ? JSON.parse(raw) : {}
    } catch {
      data = { success: false, message: 'Invalid server response' }
    }

    if (!response.ok || !data.success) {
      const error = new Error(data.message || data.error || 'Request failed')
      error.status = response.status
      error.data = data
      throw error
    }

    return data
  }

  listUsers() {
    return this.post('/admin/api/users/list')
  }

  createUser({ username, password, expires = null, hwid = null, isBanned = false }) {
    return this.post('/admin/api/users/create', {
      username,
      password,
      expires,
      hwid,
      isBanned
    })
  }

  updateUser({
    username,
    password,
    expires,
    hwid,
    isBanned,
    resetHwid
  }) {
    return this.post('/admin/api/users/update', {
      username,
      password,
      expires,
      hwid,
      isBanned,
      resetHwid
    })
  }

  resetHwid({ username, password }) {
    return this.post('/admin/api/users/reset-hwid', { username, password })
  }

  deleteUser({ username }) {
    return this.post('/admin/api/users/delete', { username })
  }
}

/**
 * Example: connect from your website server (Express)
 *
 * app.post('/api/my-site/create-license-user', async (req, res) => {
 *   try {
 *     const api = new SG71UserApi({
 *       apiBase: process.env.SG71_API_BASE,
 *       adminId: process.env.SG71_ADMIN_ID,
 *       appName: process.env.SG71_APP_NAME,
 *       apiKey: process.env.SG71_API_KEY
 *     })
 *     const result = await api.createUser({
 *       username: req.body.username,
 *       password: req.body.password,
 *       expires: req.body.expires
 *     })
 *     res.json(result)
 *   } catch (err) {
 *     res.status(err.status || 500).json({ success: false, message: err.message })
 *   }
 * })
 */
