import { useState } from "react";
import { getRandomStory, getStoryById } from "../api/stories";
import StoryModal from "./StoryModal";
import "./Bookshelf.css";

export default function Bookshelf() {
  const [selectedStory, setSelectedStory] = useState(null);
  const [loading, setLoading] = useState(false);

  const handleRandomStory = async () => {
    try {
      setLoading(true);
      const data = await getRandomStory();
      setSelectedStory(data);
    } catch (err) {
      console.error("Error fetching random story:", err);
    } finally {
      setLoading(false);
    }
  };

  const handleBookClick = async (bookId) => {
    // Map existing IDs if necessary, or just use IDs directly
    // This part depends on your specific mapping, using a mock ID for now
    try {
      setLoading(true);
      // Map your existing string IDs to database numeric IDs if needed
      const idMap = {
        "last-bus-home": 3,
        "customer-support": 2,
        "lighthouse-keeper": 9,
        "garden-of-second-chances": 7,
        "warranty-void": 11,
        "red-shift": 4,
        "clockmakers-apprentice": 5,
        "recipe-for-rain": 1,
        "silent-orchard": 10,
        "inheritance": 8
      };
      
      const dbId = idMap[bookId];
      if (dbId) {
        const data = await getStoryById(dbId);
        setSelectedStory(data);
      } else {
        handleRandomStory();
      }
    } catch (err) {
      console.error("Error fetching story:", err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="shelf-wrapper">
      <div 
        className={`shelf-image-container ${loading ? 'loading' : ''}`}
        onClick={handleRandomStory}
        title="Click for a random story"
      >
        <img src="/bilde.png" className="shelf-image" alt="Bookshelf" />
        <div className="shelf-overlay">
          <span>Discover a Random Story</span>
        </div>
      </div>

      {/* ---------- TOP SHELF ---------- */}
      <div
        className="hotspot"
        style={{ top: "10%", left: "53%", width: "10%", height: "10%" }}
        onClick={(e) => { e.stopPropagation(); handleBookClick("meta-data"); }}
      />

      <div
        className="hotspot"
        style={{ top: "10%", left: "63%", width: "10%", height: "10%" }}
        onClick={(e) => { e.stopPropagation(); handleBookClick("last-bus-home"); }}
      />

      <div
        className="hotspot"
        style={{ top: "10%", left: "73%", width: "10%", height: "10%" }}
        onClick={(e) => { e.stopPropagation(); handleBookClick("customer-support"); }}
      />

      <div
        className="hotspot"
        style={{ top: "10%", left: "83%", width: "10%", height: "10%" }}
        onClick={(e) => { e.stopPropagation(); handleBookClick("red-book"); }}
      />

      {/* ---------- SECOND SHELF ---------- */}
      <div
        className="hotspot"
        style={{ top: "28%", left: "8%", width: "28%", height: "5%" }}
        onClick={(e) => { e.stopPropagation(); handleBookClick("lighthouse-keeper"); }}
      />

      <div
        className="hotspot"
        style={{ top: "33%", left: "8%", width: "28%", height: "5%" }}
        onClick={(e) => { e.stopPropagation(); handleBookClick("garden-of-second-chances"); }}
      />

      <div
        className="hotspot"
        style={{ top: "28%", left: "60%", width: "28%", height: "10%" }}
        onClick={(e) => { e.stopPropagation(); handleBookClick("warranty-void"); }}
      />

      {/* ---------- THIRD SHELF ---------- */}
      <div
        className="hotspot"
        style={{ top: "47%", left: "20%", width: "26%", height: "5%" }}
        onClick={(e) => { e.stopPropagation(); handleBookClick("red-shift"); }}
      />

      <div
        className="hotspot"
        style={{ top: "52%", left: "20%", width: "34%", height: "5%" }}
        onClick={(e) => { e.stopPropagation(); handleBookClick("clockmakers-apprentice"); }}
      />

      {/* ---------- FOURTH SHELF ---------- */}
      <div
        className="hotspot"
        style={{ top: "67%", left: "16%", width: "30%", height: "6%" }}
        onClick={(e) => { e.stopPropagation(); handleBookClick("recipe-for-rain"); }}
      />

      <div
        className="hotspot"
        style={{ top: "73%", left: "16%", width: "30%", height: "6%" }}
        onClick={(e) => { e.stopPropagation(); handleBookClick("silent-orchard"); }}
      />

      {selectedStory && (
        <StoryModal 
          story={selectedStory} 
          onClose={() => setSelectedStory(null)} 
        />
      )}
    </div>
  );
}
