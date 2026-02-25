import { useCallback } from 'react';
import { fetchBlob, ApiError } from '../../../shared/api/client';
import { DiagramData, DiagramNode } from '../types';

const NODE_WIDTH = 220;
const NODE_HEIGHT = 80;
const PADDING = 80;

function healthColor(health: DiagramNode['health']): string {
  return health === 'green' ? '#2e8f5e' : health === 'yellow' ? '#9d7c35' : '#9e3a3a';
}

function trafficColor(traffic: number): string {
  return traffic >= 0.8 ? '#2e8f5e' : traffic >= 0.5 ? '#9d7c35' : '#9e3a3a';
}

function escapeXml(value: string): string {
  return value
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&apos;');
}

function buildSvg(data: DiagramData): string {
  const positioned = data.nodes.map((node) => ({
    ...node,
    x: (node.position?.x ?? 0) + PADDING,
    y: (node.position?.y ?? 0) + PADDING,
  }));

  const maxX = positioned.reduce((max, n) => Math.max(max, n.x + NODE_WIDTH), 0);
  const maxY = positioned.reduce((max, n) => Math.max(max, n.y + NODE_HEIGHT), 0);
  const width = maxX + PADDING;
  const height = maxY + PADDING;

  const positionById = new Map(positioned.map((n) => [n.id, n]));

  const edgesSvg = data.edges
    .map((edge) => {
      const src = positionById.get(edge.from);
      const dst = positionById.get(edge.to);
      if (!src || !dst) return '';
      const x1 = src.x + NODE_WIDTH / 2;
      const y1 = src.y + NODE_HEIGHT;
      const x2 = dst.x + NODE_WIDTH / 2;
      const y2 = dst.y;
      const color = trafficColor(edge.traffic);
      const mx = (x1 + x2) / 2;
      const my = (y1 + y2) / 2;
      return `<line x1="${x1}" y1="${y1}" x2="${x2}" y2="${y2}" stroke="${color}" stroke-width="2" marker-end="url(#arrow-${color.slice(1)})"/>
<text x="${mx}" y="${my - 4}" font-family="Inter,sans-serif" font-size="10" fill="${color}" text-anchor="middle">${Math.round(edge.traffic * 100)}%</text>`;
    })
    .join('\n');

  const nodesSvg = positioned
    .map((node) => {
      const color = healthColor(node.health);
      return `<g transform="translate(${node.x},${node.y})">
  <rect width="${NODE_WIDTH}" height="${NODE_HEIGHT}" rx="8" ry="8" fill="#1a2744" stroke="${color}" stroke-width="2"/>
  <text x="12" y="26" font-family="Inter,sans-serif" font-size="13" font-weight="600" fill="#e0e6f0">${escapeXml(node.label)}</text>
  <text x="12" y="44" font-family="Inter,sans-serif" font-size="11" fill="#8896b3">${escapeXml(node.level)} · ${escapeXml(node.serviceType)}</text>
  <text x="${NODE_WIDTH - 12}" y="26" font-family="Inter,sans-serif" font-size="10" fill="${color}" text-anchor="end">${node.health.toUpperCase()}</text>
  ${node.drift ? `<text x="12" y="66" font-family="Inter,sans-serif" font-size="10" fill="#f4a261">DRIFT</text>` : ''}
</g>`;
    })
    .join('\n');

  const arrowColors = ['2e8f5e', '9d7c35', '9e3a3a'];
  const defs = arrowColors
    .map(
      (c) =>
        `<marker id="arrow-${c}" markerWidth="8" markerHeight="8" refX="6" refY="3" orient="auto">
  <path d="M0,0 L0,6 L8,3 z" fill="#${c}"/>
</marker>`,
    )
    .join('\n');

  return `<?xml version="1.0" encoding="UTF-8"?>
<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}" viewBox="0 0 ${width} ${height}">
<defs>${defs}</defs>
<rect width="${width}" height="${height}" fill="#0f1b2d"/>
${edgesSvg}
${nodesSvg}
</svg>`;
}

function downloadBlob(blob: Blob, filename: string) {
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  a.click();
  URL.revokeObjectURL(url);
}

function isApiError(value: unknown): value is ApiError {
  return value instanceof ApiError;
}

export function useDiagramExport(data: DiagramData, projectId?: string) {
  const exportAs = useCallback(
    async (format: 'svg' | 'pdf') => {
      if (projectId !== undefined) {
        try {
          const blob = await fetchBlob(
            `/api/projects/${projectId}/diagram/export?format=${format}`,
          );
          downloadBlob(blob, `architecture-diagram.${format}`);
          return;
        } catch (err: unknown) {
          if (isApiError(err)) {
            console.warn(`Backend export failed (${err.status}), falling back to local export`);
          }
        }
      }

      const svg = buildSvg(data);
      if (format === 'svg') {
        const blob = new Blob([svg], { type: 'image/svg+xml' });
        downloadBlob(blob, 'architecture-diagram.svg');
      } else {
        const win = window.open('', '_blank');
        if (!win) return;
        win.document.write(
          `<!DOCTYPE html><html><head><title>Architecture Diagram</title>
<style>body{margin:0;background:#0f1b2d}@media print{body{margin:0}}</style></head>
<body>${svg}<script>window.onload=()=>window.print()<\/script></body></html>`,
        );
        win.document.close();
      }
    },
    [data, projectId],
  );

  return { exportAs };
}
