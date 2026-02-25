# Architecture Overview

C4 follows three complementary architectural patterns that together enable a clean, maintainable, and testable codebase. These patterns are not alternatives – they are applied simultaneously at different levels of granularity.

## The Three Lenses

| Pattern | Granularity | Primary Benefit |
|---|---|---|
| **Vertical Slice Architecture** | Feature level | Feature isolation, parallel development |
| **Modular Monolith** | Module level | Bounded contexts, future extraction |
| **Ports and Adapters** | Component level | Testability, technology independence |

## How They Compose

```
┌─────────────────────────────────────────────────────────┐
│  Modular Monolith                                        │
│  ┌──────────────────┐  ┌──────────────────┐             │
│  │  Module: Ordering │  │  Module: Catalog │  ...        │
│  │                  │  │                  │             │
│  │  ┌────────────┐  │  │  ┌────────────┐  │             │
│  │  │  Slice:    │  │  │  │  Slice:    │  │             │
│  │  │ PlaceOrder │  │  │  │ AddProduct │  │             │
│  │  │            │  │  │  │            │  │             │
│  │  │  Cmd/Qry   │  │  │  │  Cmd/Qry   │  │             │
│  │  │  Handler   │  │  │  │  Handler   │  │             │
│  │  │  Endpoint  │  │  │  │  Endpoint  │  │             │
│  │  └─────┬──────┘  │  │  └─────┬──────┘  │             │
│  │        │ port    │  │        │ port    │             │
│  │  ┌─────▼──────┐  │  │  ┌─────▼──────┐  │             │
│  │  │  Adapter   │  │  │  │  Adapter   │  │             │
│  │  │ (EF Core)  │  │  │  │ (EF Core)  │  │             │
│  │  └────────────┘  │  │  └────────────┘  │             │
│  └──────────────────┘  └──────────────────┘             │
│                                                          │
│  ┌──────────────────────────────────────────────────┐   │
│  │  Shared Kernel (value objects, base types only)   │   │
│  └──────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
```

## Decision Records

| # | Decision | Status |
|---|---|---|
| ADR-001 | Use Vertical Slice Architecture over Layer Architecture | Accepted |
| ADR-002 | Modular Monolith over Microservices for initial PoC | Accepted |
| ADR-003 | Ports and Adapters within each module | Accepted |
| ADR-004 | Semantic Kernel as AI orchestration framework | Accepted |
| ADR-005 | MediatR for in-process command/query dispatch | Accepted |
| ADR-006 | No code comments – self-documenting code only | Accepted |
| ADR-007 | Result types over exceptions for expected failures | Accepted |

## Dependency Rules

1. **Domain** depends on nothing
2. **Application** depends on Domain only
3. **Infrastructure** depends on Application and Domain (implements ports)
4. **Api** depends on Application (dispatches commands/queries)
5. **Tests** depend on Application and may reference Infrastructure for integration tests

Cross-module dependencies are only permitted at the **infrastructure/messaging** level through defined contracts (events, DTOs over module boundary).
