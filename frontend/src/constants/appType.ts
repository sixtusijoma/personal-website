export const AppType = {
  Resume: "Resume",
  Console: "Console",
  LinkedIn: "LinkedIn",
  Github: "Github"
} as const;

export type AppType = (typeof AppType)[keyof typeof AppType];
