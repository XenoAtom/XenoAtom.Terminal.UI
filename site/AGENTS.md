---
title: Website (lunet) Contribution Instructions
---

# Website (lunet) Contribution Instructions

This folder contains the static website for XenoAtom.Terminal.UI, built with **lunet**.

## Structure

- `site/readme.md` -> home page (`/`)
- `site/docs/**` -> documentation section (`/docs/**`)
  - `site/docs/menu.yml` -> sidebar menu for docs pages
  - `site/docs/controls/**` -> controls reference (`/docs/controls/**`)
  - `site/docs/controls/menu.yml` -> sidebar menu for controls pages
  - `site/docs/specs/**` -> specs/design notes (`/docs/specs/**`)
- `site/menu.yml` -> top navigation (navbar)
- `site/.lunet/**` -> layouts, CSS, JS, and build output
  - `site/.lunet/layouts/**` -> Scriban HTML layouts
  - `site/.lunet/css/**` / `site/.lunet/js/**` -> site assets
  - `site/.lunet/build/**` -> generated output (cache + `www/`)

## Building the website

Install lunet (once):

`dotnet tool install -g lunet`

Build the site (always run this after changing `site/**`):

`lunet build`

Run from the `site` folder.

## Generating control screenshots

The controls documentation uses auto-generated SVG screenshots rendered from the ControlsDemo.

Regenerate all screenshots (run from the repo root):

`dotnet run -c Release --project samples/ControlsDemo -- --export-screenshots`

Outputs are written under `site/img/controls/**`.

## Notes for agents

- `readme.md` in a folder becomes the folder's `index.html`. Keep these `readme.md` files compatible with both GitHub and the website.
- Update menus when adding/moving pages:
  - `site/menu.yml` (top nav)
  - `site/docs/menu.yml` (docs sidebar)
  - `site/docs/controls/menu.yml` (controls sidebar)
- Prefer stable, readable URLs. Add new docs under `site/docs/**`.

