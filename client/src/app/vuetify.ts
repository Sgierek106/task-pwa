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
    themes: {
      light: {
        colors: {
          primary: "#1976D2",
          secondary: "#424242",
          accent: "#82B1FF",
          error: "#FF5252",
          info: "#2196F3",
          success: "#4CAF50",
          warning: "#FFC107",
        },
      },
      dark: {
        dark: true,
        colors: {
          primary: "#90CAF9",
          secondary: "#B0BEC5",
          accent: "#82B1FF",
          error: "#EF9A9A",
          info: "#81D4FA",
          success: "#A5D6A7",
          warning: "#FFE082",
          background: "#121212",
          surface: "#1E1E1E",
        },
      },
    },
  },
  icons: {
    defaultSet: "mdi",
  },
});
