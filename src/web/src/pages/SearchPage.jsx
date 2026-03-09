import { useCallback, useEffect, useRef, useState } from "react";
import { useLocation } from "react-router-dom";
import SearchBar from "../components/SearchBar";
import ResultsGrid from "../components/ResultsGrid";
import StoryModal from "../components/StoryModal";
import { useSearch } from "../hooks/useSearch";
import { getAllStories } from "../api/stories";
import styles from "./SearchPage.module.css";

const HOTSPOTS_STORAGE_KEY = "hotspots_v1";
const DEFAULT_HOTSPOTS = [
  { id: "h1", left: 12, top: 58 },
  { id: "h2", left: 36, top: 42 },
  { id: "h3", left: 60, top: 70 },
  { id: "h4", left: 74, top: 36 },
  { id: "h5", left: 86, top: 58 },
];

// Clamp pointer coordinates to the image bounds.
function clamp01(value) {
  return Math.max(0, Math.min(1, value));
}

// Convert normalized value (0..1) to a CSS percentage value.
function toPercent(value01) {
  return Math.round(value01 * 100);
}

// Load persisted hotspots once during initial render.
function readHotspotsFromStorage() {
  try {
    const raw = localStorage.getItem(HOTSPOTS_STORAGE_KEY);
    if (!raw) return DEFAULT_HOTSPOTS;

    const saved = JSON.parse(raw);
    if (!Array.isArray(saved)) return DEFAULT_HOTSPOTS;

    // Keep user-edited positions, but auto-append any new defaults by id.
    const merged = [...saved];
    const savedIds = new Set(saved.map((hotspot) => hotspot.id));

    for (const hotspot of DEFAULT_HOTSPOTS) {
      if (!savedIds.has(hotspot.id)) {
        merged.push(hotspot);
      }
    }

    return merged;
  } catch {
    return DEFAULT_HOTSPOTS;
  }
}

export default function SearchPage() {
  const location = useLocation();
  const { results, loading, error, hasSearched, lastQuery, search, mode, setMode } =
    useSearch();

  const resultsRef = useRef(null);
  const sideRef = useRef(null);
  const draggingRef = useRef(null);

  const [selectedStory, setSelectedStory] = useState(null);
  const [hotspots, setHotspots] = useState(readHotspotsFromStorage);
  const [isEditing, setIsEditing] = useState(false);

  // If navigation provides a pre-filled query, run it on page load.
  useEffect(() => {
    if (location.state?.initialSearch) {
      search(location.state.initialSearch, mode);
    }
  }, [location.state?.initialSearch]);

  // Scroll to the results section after each completed search.
  useEffect(() => {
    if (hasSearched && !loading && resultsRef.current) {
      resultsRef.current.scrollIntoView({ behavior: "smooth", block: "start" });
    }
  }, [hasSearched, loading]);

  const updateHotspotPosition = useCallback((id, left, top) => {
    setHotspots((prev) =>
      prev.map((hotspot) =>
        hotspot.id === id ? { ...hotspot, left, top } : hotspot
      )
    );
  }, []);

  // Cleanup global listeners in case the component unmounts during a drag.
  useEffect(() => {
    return () => {
      window.removeEventListener("pointermove", onPointerMove);
      window.removeEventListener("pointerup", endDrag);
    };
  }, []);

  async function handleHotspotClick() {
    // Use current results first, then fall back to all stories.
    try {
      let pool = Array.isArray(results) && results.length ? results : null;
      if (!pool) {
        pool = await getAllStories();
      }

      if (!pool || pool.length === 0) return;

      const rand = pool[Math.floor(Math.random() * pool.length)];
      setSelectedStory(rand);
    } catch {
      // Ignore errors silently for hotspot interactions.
    }
  }

  function startDrag(e, id) {
    if (!isEditing) return;

    e.preventDefault();
    draggingRef.current = { id };
    window.addEventListener("pointermove", onPointerMove);
    window.addEventListener("pointerup", endDrag);
  }

  function onPointerMove(e) {
    if (!draggingRef.current) return;

    const el = sideRef.current;
    if (!el) return;

    const rect = el.getBoundingClientRect();
    const x = clamp01((e.clientX - rect.left) / rect.width);
    const y = clamp01((e.clientY - rect.top) / rect.height);

    updateHotspotPosition(draggingRef.current.id, toPercent(x), toPercent(y));
  }

  function endDrag() {
    draggingRef.current = null;
    window.removeEventListener("pointermove", onPointerMove);
    window.removeEventListener("pointerup", endDrag);
  }

  function saveHotspots() {
    try {
      localStorage.setItem(HOTSPOTS_STORAGE_KEY, JSON.stringify(hotspots));
      setIsEditing(false);
    } catch {
      // ignore
    }
  }

  function resetHotspots() {
    setHotspots(DEFAULT_HOTSPOTS);
    localStorage.removeItem(HOTSPOTS_STORAGE_KEY);
  }

  function addHotspotAtEvent(e) {
    if (!isEditing) return;

    // Only respond when clicking the side container itself, not controls.
    if (e.target !== sideRef.current) return;

    const rect = sideRef.current.getBoundingClientRect();
    const x = clamp01((e.clientX - rect.left) / rect.width);
    const y = clamp01((e.clientY - rect.top) / rect.height);
    const id = `h${Date.now().toString(36)}`;

    setHotspots((prev) => [...prev, { id, left: toPercent(x), top: toPercent(y) }]);
  }

  function removeHotspot(id) {
    setHotspots((prev) => prev.filter((hotspot) => hotspot.id !== id));
  }

  return (
    <div className={styles.pageWithImage}>
      <div className={styles.contentArea}>
        <div className={styles.heroWrap}>
          <SearchBar
            onSearch={search}
            loading={loading}
            mode={mode}
            onModeChange={setMode}
          />
        </div>

        {error && (
          <div className={styles.error}>
            <strong>Error:</strong> {error}
          </div>
        )}

        {loading && (
          <div className={styles.loadingWrap}>
            <div className={styles.loadingDots}>
              <span />
              <span />
              <span />
            </div>
            <p>Searching the collection...</p>
          </div>
        )}

        <div ref={resultsRef}>
          {!loading && hasSearched && (
            <ResultsGrid
              results={results}
              query={lastQuery}
              onStoryClick={setSelectedStory}
            />
          )}
        </div>

        {selectedStory && (
          <StoryModal story={selectedStory} onClose={() => setSelectedStory(null)} />
        )}
      </div>

      <div
        className={styles.sideImage}
        ref={sideRef}
        onPointerDown={addHotspotAtEvent}
      >
        {!isEditing && (
          <button
            className={styles.editToggle}
            onClick={() => setIsEditing(true)}
            aria-label="Edit hotspots"
          >
            Edit
          </button>
        )}

        {hotspots.map((hotspot) => (
          <button
            key={hotspot.id}
            className={`${styles.hotspot} ${isEditing ? styles.editing : ""}`}
            style={{ left: `${hotspot.left}%`, top: `${hotspot.top}%` }}
            onClick={handleHotspotClick}
            onPointerDown={(e) => startDrag(e, hotspot.id)}
            aria-label={`Hotspot ${hotspot.id}`}
            title={isEditing ? `${hotspot.left}%, ${hotspot.top}%` : ""}
          />
        ))}

        {isEditing && (
          <div className={styles.hotspotEditor}>
            <div className={styles.editorRow}>
              <button onClick={() => setIsEditing(false)}>Done</button>
              <button onClick={saveHotspots}>Save</button>
              <button onClick={resetHotspots}>Reset</button>
            </div>

            <div className={styles.editorList}>
              {hotspots.map((hotspot) => (
                <div key={hotspot.id} className={styles.editorItem}>
                  <strong>{hotspot.id}</strong>
                  <label>
                    L:
                    <input
                      type="number"
                      value={hotspot.left}
                      onChange={(e) =>
                        updateHotspotPosition(
                          hotspot.id,
                          Math.max(0, Math.min(100, Number(e.target.value) || 0)),
                          hotspot.top
                        )
                      }
                    />
                  </label>
                  <label>
                    T:
                    <input
                      type="number"
                      value={hotspot.top}
                      onChange={(e) =>
                        updateHotspotPosition(
                          hotspot.id,
                          hotspot.left,
                          Math.max(0, Math.min(100, Number(e.target.value) || 0))
                        )
                      }
                    />
                  </label>
                  <button
                    className={styles.removeHotspot}
                    onClick={() => removeHotspot(hotspot.id)}
                  >
                    Remove
                  </button>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
