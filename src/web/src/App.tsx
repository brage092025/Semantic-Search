import { useState } from 'react'
import { Search, Cpu, Hash, Layers, Info } from 'lucide-react'
import './App.css'

interface Story {
  id: number;
  title: string;
  author: string;
  genre: string;
  publishedYear: number;
  summary: string;
  content: string;
}

interface SearchResult {
  story: Story;
  score: number;
}

type SearchMode = 'Semantic' | 'Keyword' | 'Hybrid';

function App() {
  const [query, setQuery] = useState('');
  const [mode, setMode] = useState<SearchMode>('Hybrid');
  const [results, setResults] = useState<SearchResult[]>([]);
  const [loading, setLoading] = useState(false);
  const [selectedStory, setSelectedStory] = useState<Story | null>(null);

  const handleSearch = async (e?: React.FormEvent) => {
    if (e) e.preventDefault();
    if (!query.trim()) return;

    setLoading(true);
    try {
      const apiUrl = import.meta.env.VITE_API_URL || 'http://localhost:5041';
      console.log('Fetching from:', apiUrl);
      
      const response = await fetch(`${apiUrl}/api/stories/search`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          query,
          mode: mode, // Send string directly
          limit: 10
        })
      });
      const data = await response.json();
      console.log('Search results received:', data);
      if (data && data.length > 0) {
        console.log('First result keys:', Object.keys(data[0]));
        if (data[0].story || data[0].Story) {
          console.log('First story keys:', Object.keys(data[0].story || data[0].Story));
        }
      }
      
      // Ensure results is an array
      const resultsArray = Array.isArray(data) ? data : [];
      setResults(resultsArray);
      
      if (resultsArray.length === 0) {
        console.warn('No results returned from API');
      }
    } catch (error) {
      console.error('Search failed:', error);
      setResults([]);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="container">
      <header>
        <h1>Semantic Search</h1>
        <p>Explore stories using AI-powered hybrid search</p>
      </header>

      <main>
        <form onSubmit={handleSearch} className="search-box">
          <div className="input-group">
            <Search className="search-icon" size={20} />
            <input
              type="text"
              placeholder="What are you looking for?"
              value={query}
              onChange={(e) => setQuery(e.target.value)}
            />
            <button type="submit" disabled={loading}>
              {loading ? 'Searching...' : 'Search'}
            </button>
          </div>

          <div className="mode-selector">
            <button
              type="button"
              className={mode === 'Semantic' ? 'active' : ''}
              onClick={() => setMode('Semantic')}
            >
              <Cpu size={16} /> Semantic
            </button>
            <button
              type="button"
              className={mode === 'Keyword' ? 'active' : ''}
              onClick={() => setMode('Keyword')}
            >
              <Hash size={16} /> Keyword
            </button>
            <button
              type="button"
              className={mode === 'Hybrid' ? 'active' : ''}
              onClick={() => setMode('Hybrid')}
            >
              <Layers size={16} /> Hybrid
            </button>
          </div>
        </form>

        <div className="results-container">
          {loading ? (
            <div className="loading-state">
              <div className="spinner"></div>
              <p>Searching through stories...</p>
            </div>
          ) : results && results.length > 0 ? (
            <div className="results-list">
              {results.map((result: any, idx) => {
                // Universal mapping: handle { story: {...}, score: 10 } OR just { id: 1, title: "..." }
                const story = result.story || result.Story || (result.title || result.Title ? result : null);
                const score = result.score !== undefined ? result.score : (result.Score !== undefined ? result.Score : 100);
                
                if (!story) return null;

                const title = story.title || story.Title || 'Untitled';
                const author = story.author || story.Author || 'Unknown Author';
                const genre = story.genre || story.Genre || 'Unknown Genre';
                const year = story.publishedYear || story.PublishedYear;
                const summary = story.summary || story.Summary || 'No summary available.';
                const id = story.id || story.Id || idx;

                return (
                  <div key={id} className="result-card" onClick={() => setSelectedStory(story)}>
                    <div className="result-header">
                      <h3>{title}</h3>
                      <div className="score-badge">
                        {Math.round(score)}% Match
                      </div>
                    </div>
                    <div className="result-meta">
                      <span className="author">{author}</span>
                      <span className="dot">•</span>
                      <span className="genre">{genre}</span>
                      <span className="dot">•</span>
                      <span className="year">{year}</span>
                    </div>
                    <p className="summary">{summary}</p>
                  </div>
                );
              })}
            </div>
          ) : query && (
            <div className="no-results">
              <Info size={48} />
              <p>No matches found for your query.</p>
              <p className="hint">Try searching for keywords like "lighthouse", "clockmaker", or "space".</p>
            </div>
          )}
        </div>
      </main>

      {selectedStory && (
        <div className="modal-overlay" onClick={() => setSelectedStory(null)}>
          <div className="modal-content" onClick={e => e.stopPropagation()}>
            <div className="modal-header">
              <div>
                <h2>{selectedStory.title || (selectedStory as any).Title}</h2>
                <p>{selectedStory.author || (selectedStory as any).Author} | {selectedStory.genre || (selectedStory as any).Genre} | {selectedStory.publishedYear || (selectedStory as any).PublishedYear}</p>
              </div>
              <button className="close-btn" onClick={() => setSelectedStory(null)}>&times;</button>
            </div>
            <div className="modal-body">
              <div className="story-content">
                {selectedStory.content?.split('\n').map((para, i) => (
                  <p key={i}>{para}</p>
                ))}
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

export default App
