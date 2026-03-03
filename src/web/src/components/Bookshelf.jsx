import { useState } from "react";
import "./Bookshelf.css";

export default function Bookshelf() {
  const [story, setStory] = useState(null);

  const handleClick = async (bookId) => {
    const res = await fetch(`/api/random-story?book=${bookId}`);
    const data = await res.json();
    setStory(data.story);
  };

  return (
    <div className="shelf-wrapper">
      <img src="/bilde.png" className="shelf-image" alt="Bookshelf" />

      {/* ---------- TOP SHELF ---------- */}
      <div
        className="hotspot"
        style={{ top: "10%", left: "53%", width: "10%", height: "10%" }}
        onClick={() => handleClick("meta-data")}
      />

      <div
        className="hotspot"
        style={{ top: "10%", left: "63%", width: "10%", height: "10%" }}
        onClick={() => handleClick("last-bus-home")}
      />

      <div
        className="hotspot"
        style={{ top: "10%", left: "73%", width: "10%", height: "10%" }}
        onClick={() => handleClick("customer-support")}
      />

      <div
        className="hotspot"
        style={{ top: "10%", left: "83%", width: "10%", height: "10%" }}
        onClick={() => handleClick("red-book")}
      />

      {/* ---------- SECOND SHELF ---------- */}
      <div
        className="hotspot"
        style={{ top: "28%", left: "8%", width: "28%", height: "5%" }}
        onClick={() => handleClick("lighthouse-keeper")}
      />

      <div
        className="hotspot"
        style={{ top: "33%", left: "8%", width: "28%", height: "5%" }}
        onClick={() => handleClick("garden-of-second-chances")}
      />

      <div
        className="hotspot"
        style={{ top: "28%", left: "60%", width: "28%", height: "10%" }}
        onClick={() => handleClick("warranty-void")}
      />

      {/* ---------- THIRD SHELF ---------- */}
      <div
        className="hotspot"
        style={{ top: "47%", left: "20%", width: "26%", height: "5%" }}
        onClick={() => handleClick("red-shift")}
      />

      <div
        className="hotspot"
        style={{ top: "52%", left: "20%", width: "34%", height: "5%" }}
        onClick={() => handleClick("clockmakers-apprentice")}
      />

      {/* ---------- FOURTH SHELF ---------- */}
      <div
        className="hotspot"
        style={{ top: "67%", left: "16%", width: "30%", height: "6%" }}
        onClick={() => handleClick("recipe-for-rain")}
      />

      <div
        className="hotspot"
        style={{ top: "73%", left: "16%", width: "30%", height: "6%" }}
        onClick={() => handleClick("silent-orchard")}
      />

      {story && (
        <div className="story-box">
          <h2>Story</h2>
          <p>{story}</p>
        </div>
      )}
    </div>
  );
}
