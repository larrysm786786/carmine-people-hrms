// Every date field in this app is stored as a plain 'YYYY-MM-DD' string (what the backend
// expects and what native <input type="date"> used to produce directly), but mat-datepicker
// only understands Date objects. These convert at the edge so components can keep their
// string fields/API calls untouched and just add a Date-typed getter/setter for the picker.
export function dateToIso(date: Date | null): string {
  if (!date) return '';
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

export function isoToDate(value: string | null | undefined): Date | null {
  if (!value) return null;
  const [y, m, d] = value.slice(0, 10).split('-').map(Number);
  if (!y || !m || !d) return null;
  return new Date(y, m - 1, d);
}
