<script setup lang="ts">
import DefaultTheme from 'vitepress/theme'
import Giscus from "./components/giscus.vue";
const { Layout } = DefaultTheme
</script>

<template>
  <Layout>
    <template #doc-after>
      <Giscus/>
    </template>
  </Layout>
</template>

<style scoped>

</style>