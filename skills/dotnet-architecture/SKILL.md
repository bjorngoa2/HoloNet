---
name: dotnet-architecture
description: Reference library for .NET/ASP.NET Core application architecture guidance and Framework Design Guidelines (naming, type/member design, exceptions, extensibility, common patterns), sourced from Microsoft Learn. Use this when making architectural decisions for ASP.NET Core apps/services, or when reviewing/writing public API surface (classes, members, exceptions) for idiomatic .NET conventions.
license: Reference material sourced from Microsoft Learn (learn.microsoft.com); used for personal/educational reference only.
---

# .NET Architecture & Framework Design Guidelines Reference

This skill gives you a searchable, offline copy of two Microsoft Learn
guides:

1. **Architect modern web applications with ASP.NET Core and Azure** — an
   e-book on monolithic web app architecture: layering, separation of
   concerns, MVC structure, data access, testing, and Azure hosting.
2. **Framework Design Guidelines** — Microsoft's canonical guidance for
   designing idiomatic, reusable .NET APIs: naming, type/member design,
   exception design, extensibility, and common patterns like Dispose.

## When to use this

- Deciding how to structure/layer an ASP.NET Core application or service
  (e.g. where business logic should live, how to separate concerns between
  presentation, application core, and infrastructure).
- Reviewing or designing a public API surface (a service interface, a DTO,
  an exception type, a constructor) and wanting to check it against
  Microsoft's own naming/design conventions rather than personal preference.
- The user asks "is this good .NET/ASP.NET Core architecture?", "how should
  I name/design this type or member?", or "what's the idiomatic way to do
  X in .NET?".
- Explaining *why* a convention exists (e.g. why constructors should do
  minimal work, why to prefer composition over required base classes).

## How to use this

```
reference/
├── Modern Web Apps (ASP.NET Core and Azure)/   ← one file per chapter (11 files)
│   ├── Introduction.md
│   ├── Architectural principles.md
│   ├── Common web application architectures.md
│   ├── Develop ASP.NET Core MVC Apps.md
│   ├── Work with data in ASP.NET Core.md
│   ├── Test ASP.NET Core MVC Apps.md
│   └── ... (Azure hosting/dev process chapters)
└── Framework Design Guidelines/                ← one file per topic, grouped by category
    ├── Overview.md
    ├── Naming Guidelines/                      ← capitalization, naming of assemblies,
    │                                              namespaces, types, members, parameters
    ├── Type Design Guidelines/                 ← class vs struct, abstract/static classes,
    │                                              interfaces, enums, nested types
    ├── Member Design Guidelines/                ← overloading, properties, constructors,
    │                                              events, fields, extension methods, operators
    ├── Design for Extensibility/                ← unsealed classes, virtual members,
    │                                              abstractions, sealing
    ├── Exception Design Guidelines/             ← exception throwing, standard exception
    │                                              types, performance
    ├── Usage Guidelines/                        ← arrays, attributes, collections,
    │                                              serialization, equality operators
    └── Common Design Patterns/                  ← dependency properties, Dispose pattern
```

1. **Find the right file(s).** Grep/search `reference/` for the topic (e.g.
   "constructor", "exception", "layer", "separation of concerns"). Filenames
   match topic names closely. If unsure which file has what you need, check
   the relevant category's overview file first (e.g. `Naming guidelines.md`,
   `Usage guidelines.md`).
2. **Read the matching file(s)** for the DO/CONSIDER/AVOID/DO NOT guidance
   and rationale.
3. **Apply the guidance to the actual code/decision at hand**, not as a
   rigid checklist — always weigh it against the project's existing
   conventions and custom instructions, which take precedence when they
   conflict with generic guidance.
4. **Cite what you used** by name (e.g. "per the Framework Design
   Guidelines' constructor design guidance, constructors should do minimal
   work" or "this follows the layered architecture pattern from the Modern
   Web Apps guide") so the reasoning is traceable.

## Notes

- The Modern Web Apps guide assumes a **monolithic MVC** app; adapt its
  layering advice (presentation / application core / infrastructure) to
  whatever project shape you're working in — e.g. a Minimal API service
  still benefits from separating business logic from I/O, even without MVC
  controllers.
- Framework Design Guidelines is written for **public library/API authors**;
  some advice (e.g. around versioning, binary compatibility) matters less
  for internal application code — use judgment about which rules still
  apply.
- `fetch_docs.py` in this skill's root is a maintenance script that
  re-fetches and re-converts `reference/` from Microsoft Learn. You don't
  need to run it during normal use — only if the source docs are updated or
  reference files are missing.
