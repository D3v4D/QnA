import { useState } from 'react'
import ReactMarkdown from 'react-markdown'
import remarkGfm from 'remark-gfm'
import UploadKnowledge from './UploadKnowledge'
import './App.css'

function App() {
  const [question, setQuestion] = useState('')
  const [answer, setAnswer] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [activeTab, setActiveTab] = useState('ask') // 'ask' or 'upload'

  const handleSubmit = async (e) => {
    e.preventDefault()
    
    if (!question.trim()) {
      setError('Please enter a question')
      return
    }

    setLoading(true)
    setError('')
    setAnswer('')

    try {
      const response = await fetch('/api/QnA/ask', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ question: question.trim() })
      })

      if (!response.ok) {
        throw new Error('Failed to get answer')
      }

      const data = await response.json()
      setAnswer(data.answer)
    } catch (err) {
      setError(err.message || 'Something went wrong')
    } finally {
      setLoading(false)
    }
  }

  const handleClear = () => {
    setQuestion('')
    setAnswer('')
    setError('')
  }

  const handleUploadSuccess = () => {
    // Optionally show a notification or refresh knowledge base info
    console.log('Upload successful, knowledge base will be reloaded')
  }

  return (
    <div className="app">
      <div className="container">
        <h1>🤖 Knowledge Base Q&A</h1>
        <p className="subtitle">Ask questions or add new knowledge sources</p>

        {/* Tab Navigation */}
        <div className="tabs">
          <button
            className={`tab ${activeTab === 'ask' ? 'active' : ''}`}
            onClick={() => setActiveTab('ask')}
          >
            💬 Ask Question
          </button>
          <button
            className={`tab ${activeTab === 'upload' ? 'active' : ''}`}
            onClick={() => setActiveTab('upload')}
          >
            📤 Upload Knowledge
          </button>
        </div>

        {/* Ask Question Tab */}
        {activeTab === 'ask' && (
          <>
            <form onSubmit={handleSubmit}>
              <div className="input-group">
                <textarea
                  value={question}
                  onChange={(e) => setQuestion(e.target.value)}
                  placeholder="Type your question here..."
                  rows="4"
                  disabled={loading}
                />
              </div>

              <div className="button-group">
                <button type="submit" disabled={loading || !question.trim()}>
                  {loading ? '⏳ Thinking...' : '🚀 Ask Question'}
                </button>
                <button type="button" onClick={handleClear} className="clear-btn">
                  🗑️ Clear
                </button>
              </div>
            </form>

            {error && (
              <div className="error">
                ❌ {error}
              </div>
            )}

            {answer && (
              <div className="answer">
                <h2>📝 Answer:</h2>
                <div className="answer-content markdown-body">
                  <ReactMarkdown remarkPlugins={[remarkGfm]}>
                    {answer}
                  </ReactMarkdown>
                </div>
              </div>
            )}
          </>
        )}

        {/* Upload Knowledge Tab */}
        {activeTab === 'upload' && (
          <UploadKnowledge onUploadSuccess={handleUploadSuccess} />
        )}
      </div>
    </div>
  )
}

export default App

