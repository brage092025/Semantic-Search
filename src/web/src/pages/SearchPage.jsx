import { useEffect, useRef, useState } from "react";
import { useLocation } from "react-router-dom";
import SearchBar from "../components/SearchBar";
import ResultsGrid from "../components/ResultsGrid";
import StoryModal from "../components/StoryModal";
import { useSearch } from "../hooks/useSearch";
import { getAllStories } from "../api/stories";
import styles from "./SearchPage.module.css";

export default function SearchPage() {
  const location = useLocation();
  const {
    results,
    loading,
    error,
    hasSearched,
    lastQuery,
    search,
    mode,
    setMode,
  } = useSearch();

  const resultsRef = useRef(null);
  const [selectedStory, setSelectedStory] = useState(null);
  const [genreFilter, setGenreFilter] = useState("all");
  const [showTopButton, setShowTopButton] = useState(false);

  // If navigation provides a pre-filled query, run it on page load.
  useEffect(() => {
    if (location.state?.initialSearch) {
      setGenreFilter("all");
      search(location.state.initialSearch, mode);
    }
  }, [location.state?.initialSearch]);

  // Scroll to the results section after each completed search.
  useEffect(() => {
    if (hasSearched && !loading && resultsRef.current) {
      resultsRef.current.scrollIntoView({ behavior: "smooth", block: "start" });
    }
  }, [hasSearched, loading]);

  useEffect(() => {
    const handleScroll = () => {
      setShowTopButton(window.scrollY > 240);
    };

    handleScroll();
    window.addEventListener("scroll", handleScroll, { passive: true });
    return () => window.removeEventListener("scroll", handleScroll);
  }, []);

  function handleGenreClick(genre) {
    if (!genre) return;
    setGenreFilter(genre);
    setSelectedStory(null);
  }

  function handleSearch(query, searchMode) {
    setGenreFilter("all");
    search(query, searchMode);
  }

  async function handleCabinetClick() {
    if (loading) return;
    try {
      let pool = Array.isArray(results) && results.length ? results : null;
      if (!pool) {
        pool = await getAllStories();
      }

      if (!pool || pool.length === 0) return;

      const rand = pool[Math.floor(Math.random() * pool.length)];
      setSelectedStory(rand);
    } catch {
      // Ignore random-pick errors silently.
    }
  }

  function handleScrollToTop() {
    window.scrollTo({ top: 0, behavior: "smooth" });
  }

  return (
    <div className={styles.pageWithImage}>
      <div className={styles.contentArea}>
        <div className={styles.heroWrap}>
          <SearchBar
            onSearch={handleSearch}
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
              genreFilter={genreFilter}
              onGenreChange={setGenreFilter}
            />
          )}
        </div>

        {selectedStory && (
          <StoryModal
            story={selectedStory}
            onClose={() => setSelectedStory(null)}
            onGenreClick={handleGenreClick}
          />
        )}
        <button
          type="button"
          className={`${styles.toTop} ${showTopButton ? styles.toTopVisible : ""}`}
          onClick={handleScrollToTop}
          aria-label="Back to top"
        >
          &#129093;
        </button>
      </div>

      <div className={styles.sideImage} aria-hidden="true">
        <span className={styles.cabinetCta}>
          Tap the Bookshelf for a Surprise Tale!
        </span>
        <button
          type="button"
          className={styles.bookshelfHotspot}
          onClick={handleCabinetClick}
          aria-label="Tap the Bookshelf for a Surprise Tale!"
        />
      </div>
    </div>
  );
}
