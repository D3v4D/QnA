import { useState } from 'react'
import './UploadKnowledge.css'

function UploadKnowledge({ onUploadSuccess }) {
  const [selectedFile, setSelectedFile] = useState(null)
  const [url, setUrl] = useState('')
  const [uploading, setUploading] = useState(false)
  const [message, setMessage] = useState('')
  const [messageType, setMessageType] = useState('') // 'success' or 'error'

  const handleFileChange = (e) => {
    const file = e.target.files[0]
    if (file) {
      const extension = file.name.split('.').pop().toLowerCase()
      if (extension === 'pdf' || extension === 'txt') {
        setSelectedFile(file)
        setMessage('')
      } else {
        setSelectedFile(null)
        setMessage('Only PDF and TXT files are supported')
        setMessageType('error')
      }
    }
  }

  const handleFileUpload = async () => {
    if (!selectedFile) {
      setMessage('Please select a file first')
      setMessageType('error')
      return
    }

    setUploading(true)
    setMessage('')

    const formData = new FormData()
    formData.append('file', selectedFile)

    try {
      const response = await fetch('/api/KnowledgeSource/upload-file', {
        method: 'POST',
        body: formData
      })

      // Check if response is JSON
      const contentType = response.headers.get('content-type')
      if (!contentType || !contentType.includes('application/json')) {
        const text = await response.text()
        throw new Error(`Server returned non-JSON response (${response.status}): ${text.substring(0, 100)}`)
      }

      const data = await response.json()

      if (response.ok && data.success) {
        setMessage(`✅ ${data.message}: ${data.fileName}`)
        setMessageType('success')
        setSelectedFile(null)
        // Reset file input
        const fileInput = document.getElementById('fileInput')
        if (fileInput) fileInput.value = ''
        
        if (onUploadSuccess) onUploadSuccess()
      } else {
        setMessage(`❌ ${data.message || 'Upload failed'}`)
        setMessageType('error')
      }
    } catch (error) {
      setMessage(`❌ Error uploading file: ${error.message}`)
      setMessageType('error')
    } finally {
      setUploading(false)
    }
  }

  const handleUrlUpload = async () => {
    if (!url.trim()) {
      setMessage('Please enter a URL')
      setMessageType('error')
      return
    }

    setUploading(true)
    setMessage('')

    try {
      const response = await fetch('/api/KnowledgeSource/upload-url', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({ url: url.trim() })
      })

      // Check if response is JSON
      const contentType = response.headers.get('content-type')
      if (!contentType || !contentType.includes('application/json')) {
        const text = await response.text()
        throw new Error(`Server returned non-JSON response (${response.status}): ${text.substring(0, 100)}`)
      }

      const data = await response.json()

      if (response.ok && data.success) {
        setMessage(`✅ ${data.message}: ${data.fileName}`)
        setMessageType('success')
        setUrl('')
        
        if (onUploadSuccess) onUploadSuccess()
      } else {
        setMessage(`❌ ${data.message || 'Download failed'}`)
        setMessageType('error')
      }
    } catch (error) {
      setMessage(`❌ Error downloading URL: ${error.message}`)
      setMessageType('error')
    } finally {
      setUploading(false)
    }
  }

  return (
    <div className="upload-knowledge">
      <h2>📚 Add Knowledge Sources</h2>
      
      {/* File Upload Section */}
      <div className="upload-section">
        <h3>📄 Upload File (PDF or TXT)</h3>
        <div className="file-upload-container">
          <input
            id="fileInput"
            type="file"
            accept=".pdf,.txt"
            onChange={handleFileChange}
            disabled={uploading}
            className="file-input"
          />
          <button
            onClick={handleFileUpload}
            disabled={!selectedFile || uploading}
            className="upload-btn"
          >
            {uploading ? '⏳ Uploading...' : '📤 Upload File'}
          </button>
        </div>
        {selectedFile && (
          <p className="file-info">Selected: {selectedFile.name}</p>
        )}
      </div>

      {/* URL Upload Section */}
      <div className="upload-section">
        <h3>🔗 Download from URL</h3>
        <div className="url-upload-container">
          <input
            type="url"
            value={url}
            onChange={(e) => setUrl(e.target.value)}
            placeholder="https://example.com/article"
            disabled={uploading}
            className="url-input"
          />
          <button
            onClick={handleUrlUpload}
            disabled={!url.trim() || uploading}
            className="upload-btn"
          >
            {uploading ? '⏳ Downloading...' : '🌐 Download & Add'}
          </button>
        </div>
      </div>

      {/* Message Display */}
      {message && (
        <div className={`message ${messageType}`}>
          {message}
        </div>
      )}
    </div>
  )
}

export default UploadKnowledge

