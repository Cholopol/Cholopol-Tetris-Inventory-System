import { BrowserRouter as Router, Routes, Route } from 'react-router-dom'
import Layout from './components/Layout'
import Home from './pages/Home'
import GettingStarted from './pages/GettingStarted'
import Documentation from './pages/Documentation'
import ApiReference from './pages/ApiReference'
import Examples from './pages/Examples'
import Contributing from './pages/Contributing'

function App() {
  return (
    <Router basename={import.meta.env.BASE_URL}>
      <Layout>
        <Routes>
          <Route path="/" element={<Home />} />
          <Route path="/getting-started" element={<GettingStarted />} />
          <Route path="/documentation" element={<Documentation />} />
          <Route path="/api/*" element={<ApiReference />} />
          <Route path="/examples" element={<Examples />} />
          <Route path="/contributing" element={<Contributing />} />
        </Routes>
      </Layout>
    </Router>
  )
}

export default App