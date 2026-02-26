import { useState } from 'react';

const STAR_COUNT = 5;
const FILLED_STAR = '\u2605';
const EMPTY_STAR = '\u2606';

interface StarRatingProps {
  rating: number;
  onRate: (rating: number) => void;
  readonly?: boolean;
}

export function StarRating({ rating, onRate, readonly = false }: StarRatingProps) {
  const [hovered, setHovered] = useState<number | undefined>(undefined);

  const effectiveRating = hovered ?? rating;

  return (
    <div
      className="star-rating"
      role={readonly ? 'img' : 'group'}
      aria-label={`Rating: ${rating} out of ${STAR_COUNT}`}
    >
      {Array.from({ length: STAR_COUNT }, (_, index) => {
        const starValue = index + 1;
        const isFilled = starValue <= effectiveRating;

        return (
          <button
            key={starValue}
            type="button"
            className={`star-btn${isFilled ? ' star-filled' : ' star-empty'}`}
            onClick={() => !readonly && onRate(starValue)}
            onMouseEnter={() => !readonly && setHovered(starValue)}
            onMouseLeave={() => !readonly && setHovered(undefined)}
            disabled={readonly}
            aria-label={`Rate ${starValue} out of ${STAR_COUNT}`}
            aria-pressed={starValue === rating}
          >
            {isFilled ? FILLED_STAR : EMPTY_STAR}
          </button>
        );
      })}
    </div>
  );
}
