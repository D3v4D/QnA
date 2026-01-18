# QnA Frontend

Simple React frontend for the Knowledge Base Q&A system.

## Setup

1. Install dependencies:
```bash
npm install
```

2. Run the development server:
```bash
npm run dev
```

The frontend will be available at `http://localhost:3000`

## Configuration

The frontend is configured to proxy API requests to `http://localhost:5000`. 
Make sure your backend is running on port 5000.

If your backend runs on a different port, update the `vite.config.js` file.

## Build

To build for production:
```bash
npm run build
```

The built files will be in the `dist` directory.

