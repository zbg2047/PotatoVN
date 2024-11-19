// .vitepress/theme/index.ts
import type { Theme } from 'vitepress'
import DefaultTheme from 'vitepress/theme'
import MsStoreBadge from './components/MsStoreBadge.vue'
import Card from './components/Card.vue'
import { createVuetify } from 'vuetify'
import 'vuetify/styles'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'

export default {
  extends: DefaultTheme,
  enhanceApp({ app }) {
    // 注册自定义全局组件
    app.component('MsStoreBadge', MsStoreBadge)
    app.component('Card', Card)

    const vuetify = createVuetify({
      components,
      directives,
    })

    app.use(vuetify)
  }
} satisfies Theme