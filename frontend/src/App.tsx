import { Outlet } from 'react-router-dom';
import './App.css';
import './utils/reactQuillPolyfill'; // Load React 19 compatibility polyfill for react-quill

function App() {
  return (
    <div className="app">
      <Outlet />
    </div>
  );
}

export default App;
