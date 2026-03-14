import "vuetify/styles";
import "@mdi/font/css/materialdesignicons.css";
import { createVuetify } from "vuetify";
import * as components from "vuetify/components";
import * as directives from "vuetify/directives";

const THEME_STORAGE_KEY = "task-pwa-theme";

function getDefaultTheme(): "light" | "dark" {
  if (typeof window === "undefined") {
    return "light";
  }

  const storedTheme = window.localStorage.getItem(THEME_STORAGE_KEY);
  if (storedTheme === "dark" || storedTheme === "light") {
    return storedTheme;
  }

  return window.matchMedia("(prefers-color-scheme: dark)").matches
    ? "dark"
    : "light";
}

export const vuetify = createVuetify({
  components,
  directives,
  theme: {
    defaultTheme: getDefaultTheme(),
  },
  icons: {
    defaultSet: "mdi",
  },
});
