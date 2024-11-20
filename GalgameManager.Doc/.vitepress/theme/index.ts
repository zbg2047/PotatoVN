// .vitepress/theme/index.ts
import type { Theme } from 'vitepress'
import Layout from "./Layout.vue";
import DefaultTheme from 'vitepress/theme'
import MsStoreBadge from './components/MsStoreBadge.vue'
import {
  NolebaseGitChangelogPlugin
} from '@nolebase/vitepress-plugin-git-changelog/client'
import '@nolebase/vitepress-plugin-git-changelog/client/style.css'

export default {
  extends: DefaultTheme,
  Layout: Layout,
  enhanceApp({ app }) {
    app.use(NolebaseGitChangelogPlugin)
    // 注册自定义全局组件
    app.component('MsStoreBadge', MsStoreBadge)
  }
} satisfies Theme