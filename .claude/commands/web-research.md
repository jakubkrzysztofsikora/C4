Research a technical topic online and synthesize findings into actionable guidance for this codebase.

## Arguments
- `$ARGUMENTS` – topic to research (e.g., "Semantic Kernel process framework best practices", "vertical slice architecture with minimal APIs .NET 9", "React 19 concurrent features patterns")

## Instructions

1. Parse the research topic from `$ARGUMENTS`.
2. Use the WebSearch tool to find authoritative, recent sources (prioritize content from 2024–2026).
3. Search for:
   - Official documentation and release notes
   - GitHub repositories with high adoption (stars, recent commits)
   - Technical blogs from known practitioners
   - Conference talks and their summaries

4. For each source found:
   - Note the URL, author, and publication date
   - Extract key patterns, recommendations, or code examples
   - Assess relevance to this codebase's tech stack

5. Synthesize findings:
   - Identify consensus best practices (mentioned by 2+ independent sources)
   - Identify emerging patterns that are gaining traction
   - Identify anything that conflicts with current patterns in this codebase

6. Translate findings into concrete, actionable recommendations:
   - "In this codebase, apply X by doing Y in Z location"
   - Provide code examples adapted to the project's conventions
   - Flag anything that would require architectural changes

7. Produce a structured research report.

## Output Format

```
## Research: <topic>
Date: <today's date>

### Sources Consulted
1. <URL> – <Author> – <Date> – Relevance: High/Medium/Low
2. ...

### Key Findings

#### Consensus Best Practices
- <finding> (Source: N, M)

#### Emerging Patterns
- <pattern> – Adoption: Early/Growing/Mainstream

#### Conflicts with Current Codebase
- <conflict> – Current: <what we do> | Recommended: <what to do>

### Actionable Recommendations for This Codebase
1. **<Action>** – Priority: High/Medium/Low
   - Where: <files/modules affected>
   - How: <specific steps>
   ```csharp / typescript
   <example code>
   ```

### Summary
<2–3 sentence synthesis of what matters most>
```
