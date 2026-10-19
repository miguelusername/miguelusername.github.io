# miguelusername.github.io

CS-499 Computer Science Capstone ePortfolio — Miguel Gigandet
Southern New Hampshire University

**Live site:** https://miguelusername.github.io/

## Contents

| Page | What's on it |
|---|---|
| [`index.html`](index.html) | Professional self-assessment — the introduction to the portfolio |
| [`code-review.html`](code-review.html) | The code review video and what it covers |
| [`software-design.html`](software-design.html) | Enhancement One — software design and engineering |
| [`algorithms.html`](algorithms.html) | Enhancement Two — algorithms and data structures |
| [`databases.html`](databases.html) | Enhancement Three — databases |
| [`artifacts.html`](artifacts.html) | Index of all source, documents, and how to run the tests |

## Artifacts

```
artifacts/
├── frogblog/
│   ├── original/     FrogBlog before this course (CS-465 team project)
│   └── enhanced/     after the software design and database enhancements
├── zombie-taxi/
│   ├── original/     Zombie Taxi gameplay scripts before enhancement (GAM-360)
│   └── enhanced/     after the algorithms enhancement, plus the test suite
└── narratives/       the self-assessment and three enhancement narratives (DOCX)
```

Zombie Taxi is an unreleased commercial project. Only the gameplay scripts and
assembly definitions are published here; the Unity project — scenes, prefabs, art and
audio — stays private. The published code is the whole of what changed in that
enhancement.

## Enhancements at a glance

**FrogBlog — software design.** Shared authentication middleware replacing copy-pasted
session checks, a centralized error handler, Zod validation on every input,
environment-driven configuration, and a layered listener/service/model architecture.

**Zombie Taxi — algorithms and data structures.** A uniform spatial hash grid, a
round-robin update scheduler, and a distance-based level-of-detail selector, bounding
the per-frame cost of 100 agents. Measured 20× speedup on nearest-agent queries.

**FrogBlog — databases.** Comments split into their own collection, a reaction
collection with a unique compound index replacing mutable counters, indexes driven by
real query patterns, cascading deletes inside a transaction, and `$jsonSchema`
validators enforced by MongoDB itself.

## Verification

| Suite | Count | Reproducible from this repo |
|---|---|---|
| FrogBlog unit tests | 8 | Yes — `npm test` |
| FrogBlog integration tests | 23 | Yes — `npm run test:db` (needs a MongoDB replica set) |
| Zombie Taxi EditMode tests | 37 | Sources readable; the suite runs inside the private Unity project |

FrogBlog run instructions are on the [artifacts page](artifacts.html).
