# Carmine People — UX Guidelines (Phase 0)

## Design principles

Premium, modern, minimal, enterprise-grade. Not a default Material/Bootstrap
look — a deliberate design system with centralized theme tokens (colors,
spacing, typography, radii) so the whole product is white-labelable by
swapping token values, not by editing components.

## Theme tokens (proposed structure)

```
colors:  primary, secondary, surface, surface-variant, on-surface, success,
         warning, error, info — each with a light and dark value
spacing: 4/8/12/16/24/32/48 scale
radius:  sm/md/lg/pill
typography: font family, weight scale, size scale (display/headline/title/body/caption)
elevation: shadow tokens for cards/modals
```

No hardcoded hex colors or pixel values in components — always reference tokens.

## Dark mode

Proper token redefinition per theme, not a naive color inversion. Contrast
ratios checked against WCAG AA minimums in both themes.

## Components (shared vocabulary across Admin + Mobile, adapted per platform)

- Rounded cards with generous padding for KPI/summary tiles
- Skeleton loaders for any async content (never a blank flash or spinner-only for lists)
- Meaningful empty states (illustration/message + a clear next action, not just "No data")
- Meaningful error states (what happened + retry action, never a raw error code alone)
- Data tables (Admin): search, filter, sort, pagination, column visibility where useful

## Mobile (Flutter) specifics

- Custom design system, not default Material widgets styled minimally — full theme override
- Core flows: Login, Home (attendance status + quick check-in/out), Attendance
  history, Leave (apply/track), Profile, Notifications
- Touch targets >= 44x44dp, accessible contrast, screen-reader labels on interactive elements
- Offline states shown honestly — never show attendance as confirmed until the
  server has actually acknowledged it

## Admin (Angular) specifics

- SaaS-dashboard quality: KPI cards (headcount, present/absent/leave/late/WFH),
  attendance/leave/hours charts, recent-activity and pending-approvals tables
- Angular Material used selectively for complex primitives (date pickers, tables)
  but styled through theme tokens, not left as default Material look

## Accessibility

- Color contrast meets WCAG AA
- All interactive elements keyboard-navigable (Admin) and screen-reader labeled (both)
- Motion/animation respects reduced-motion preference where platform supports it

## Open questions for approval

- Confirm brand token starting values (primary/secondary color, font family) — "Carmine People" is a placeholder name/brand
- Confirm whether a component library (e.g. shared Figma or Storybook) will be maintained alongside code
