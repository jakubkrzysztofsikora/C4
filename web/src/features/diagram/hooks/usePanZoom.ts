import { useState } from 'react';

export function usePanZoom() {
  const [zoom, setZoom] = useState(1);
  const [offset, setOffset] = useState({ x: 0, y: 0 });
  return { zoom, setZoom, offset, setOffset };
}
