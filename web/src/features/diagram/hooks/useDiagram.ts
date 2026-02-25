import { useMemo, useState } from 'react';
import { DiagramData } from '../types';

const seed: DiagramData = {
  nodes: [
    { id: 'n1', label: 'Frontend SPA', level: 'Container', health: 'green', serviceType: 'app' },
    { id: 'n2', label: 'Identity API', level: 'Container', health: 'green', serviceType: 'api' },
    { id: 'n3', label: 'Discovery Worker', level: 'Component', health: 'yellow', serviceType: 'queue' },
    { id: 'n4', label: 'Graph Service', level: 'Container', health: 'green', serviceType: 'api' },
    { id: 'n5', label: 'PostgreSQL', level: 'Container', health: 'green', serviceType: 'database' },
    { id: 'n6', label: 'Redis Cache', level: 'Container', health: 'yellow', drift: true, serviceType: 'cache' },
    { id: 'n7', label: 'Azure Resource Graph', level: 'Context', health: 'green', serviceType: 'external' }
  ],
  edges: [
    { id: 'e1', from: 'n1', to: 'n2', traffic: 0.9 },
    { id: 'e2', from: 'n2', to: 'n4', traffic: 0.76 },
    { id: 'e3', from: 'n4', to: 'n5', traffic: 0.63 },
    { id: 'e4', from: 'n3', to: 'n7', traffic: 0.52 },
    { id: 'e5', from: 'n4', to: 'n6', traffic: 0.45 }
  ]
};

export function useDiagram() {
  const [level, setLevel] = useState<'Context' | 'Container' | 'Component'>('Container');
  const [search, setSearch] = useState('');
  const [timeline, setTimeline] = useState(100);

  const data = useMemo(() => {
    const levelFiltered = seed.nodes.filter((n) => level === 'Container' ? n.level !== 'Context' : n.level === level);
    const searchFiltered = levelFiltered.filter((n) => n.label.toLowerCase().includes(search.toLowerCase()));
    const visibleNodeIds = new Set(searchFiltered.map((n) => n.id));

    return {
      nodes: searchFiltered,
      edges: seed.edges.filter((e) => visibleNodeIds.has(e.from) && visibleNodeIds.has(e.to) && e.traffic <= timeline / 100)
    };
  }, [level, search, timeline]);

  return { data, level, setLevel, search, setSearch, timeline, setTimeline };
}
