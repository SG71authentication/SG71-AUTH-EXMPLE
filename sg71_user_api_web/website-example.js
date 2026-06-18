/**
 * Minimal Express server — website connects to SG71 User Management API.
 * Run on your backend only. Never put apiKey in browser code.
 *
 *   npm init -y
 *   npm install express
 *   set SG71_API_KEY=sg71_...
 *   set SG71_ADMIN_ID=your-uid
 *   set SG71_APP_NAME=MyApp
 *   node website-example.js
 */

import express from 'express'
import { SG71UserApi } from './sg71-user-api.js'

const app = express()
app.use(express.json())

function getApi() {
  return new SG71UserApi({
    apiBase: process.env.SG71_API_BASE || 'https://sg71auth.netlify.app/api',
    adminId: process.env.SG71_ADMIN_ID,
    appName: process.env.SG71_APP_NAME,
    apiKey: process.env.SG71_API_KEY
  })
}

app.get('/api/users', async (_req, res) => {
  try {
    const data = await getApi().listUsers()
    res.json(data)
  } catch (err) {
    res.status(err.status || 500).json({ success: false, message: err.message })
  }
})

app.post('/api/users', async (req, res) => {
  try {
    const { username, password, expires } = req.body || {}
    const data = await getApi().createUser({ username, password, expires })
    res.status(201).json(data)
  } catch (err) {
    res.status(err.status || 500).json({ success: false, message: err.message })
  }
})

app.post('/api/users/reset-hwid', async (req, res) => {
  try {
    const { username, password } = req.body || {}
    const data = await getApi().resetHwid({ username, password })
    res.json(data)
  } catch (err) {
    res.status(err.status || 500).json({ success: false, message: err.message })
  }
})

const port = process.env.PORT || 4000
app.listen(port, () => {
  console.log(`Website API bridge running on http://localhost:${port}`)
})
