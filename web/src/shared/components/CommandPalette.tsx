import { useSearch } from '../search/SearchContext';

export function CommandPalette() {
  const { query, setQuery } = useSearch();

  return (
    <input
      className="input"
      aria-label="Search resources, nodes, and services"
      placeholder="Search resources, nodes, and services..."
      value={query}
      onChange={(e) => setQuery(e.target.value)}
    />
  );
}
