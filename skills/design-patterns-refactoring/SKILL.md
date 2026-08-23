---
name: design-patterns-refactoring
description: Reference library for Gang of Four (GoF) design patterns, code smells, and refactoring techniques from sourcemaking.com. Use this when explaining, identifying, or applying a design pattern; when spotting or naming a code smell; or when recommending or performing a specific refactoring technique to improve code quality.
license: Reference material sourced from sourcemaking.com; used for personal/educational reference only.
---

# Design Patterns & Refactoring Reference

This skill gives you a searchable, offline copy of the SourceMaking design
patterns, code smells, and refactoring techniques catalog. Use it whenever a
task benefits from citing a *specific, named* pattern, smell, or technique
rather than a vague description.

## When to use this

- The user asks "what design pattern fits this problem?" or "is this a code
  smell?"
- You are reviewing code and want to name the exact smell you're seeing
  (e.g. "this is Feature Envy", "this is a Long Parameter List") and back it
  with a concrete refactoring technique.
- You are refactoring code and want to follow a named, well-established
  technique (e.g. Extract Method, Replace Conditional with Polymorphism)
  rather than an ad-hoc rewrite.
- The user asks you to explain a GoF pattern (Creational, Structural, or
  Behavioral) with example code.

## How to use this

The `reference/` directory mirrors the source site's structure and contains
one Markdown file per topic (converted from the original PDFs, so formatting
is plain but complete: problem/solution, before/after code, and rationale).

```
reference/
├── Design Patterns.md                     ← top-level overview
├── Creational patterns/                   ← Abstract Factory, Builder, Factory Method,
│                                             Object Pool, Prototype, Singleton
├── Structural patterns/                   ← Adapter, Bridge, Composite, Decorator,
│                                             Facade, Flyweight, Private Class Data, Proxy
├── Behavioral patterns/                   ← Chain of Responsibility, Command, Interpreter,
│                                             Iterator, Mediator, Memento, Null Object,
│                                             Observer, State, Strategy, Template Method,
│                                             Visitor
└── Refactoring/
    ├── Refactoring.md                     ← top-level overview
    ├── Code Smells/                       ← Bloaters, Change Preventers, Couplers,
    │                                         Dispensables, Object-Orientation Abusers,
    │                                         Other Smells (each with individual smell files)
    └── Refactoring techniques/            ← Composing Methods, Moving Features between
                                              Objects, Organizing Data, Simplifying
                                              Conditional Expressions, Simplifying Method
                                              Calls, Dealing with Generalisation (each with
                                              individual technique files)
```

1. **Find the right file.** Grep/search `reference/` for the pattern, smell,
   or technique name (filenames match the topic name closely, e.g.
   `Extract Method.md`, `Feature Envy.md`, `Strategy Design Pattern.md`). If
   you don't know the exact name, search the relevant category overview file
   first (e.g. `Refactoring/Code Smells/Code Smells.md`) to find the right
   sub-topic.
2. **Read the matching file(s)** for the problem/solution description,
   before/after code, and the "why" rationale.
3. **Apply it to the user's actual code**, adapting the example to their
   language and conventions — don't paste the reference example verbatim.
4. **Cite what you used** by name (e.g. "This is the Extract Class
   refactoring" or "Applying the Strategy pattern here") so the user can
   look it up themselves if they want more depth.

## Notes

- This is reference material for identification and technique lookup, not a
  style guide to enforce blindly — always weigh a suggested pattern/smell
  against the project's actual conventions and existing custom instructions.
- If a category's overview file lists sub-topics you don't have a matching
  file for, treat the overview's description as sufficient; don't fabricate
  detail beyond what's available.
- `extract_pdfs.py` in this skill's root is a maintenance script that
  regenerates `reference/` from the original PDFs (stored separately, e.g. in
  a project's `practices/` folder). You don't need to run it during normal
  use — only if the source PDFs are updated or reference files are missing.
