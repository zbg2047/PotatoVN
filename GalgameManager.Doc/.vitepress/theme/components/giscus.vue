<!--component from https://github.com/zotero-chinese/website-->
<!--Zotero Chinese-->
<!--MIT License-->

<script setup lang="ts">
import Giscus from '@giscus/vue'
import {useData} from 'vitepress'

const {frontmatter, title, isDark} = useData()
</script>

<template>
  <div
      v-if="frontmatter.comments !== false"
      :key="title"
      class="comments-container"
  >
    <Giscus
        id="comments"
        repo="GoldenPotato137/PotatoVN"
        repo-id="R_kgDOJQYT9A"
        category="文档评论区"
        category-id="DIC_kwDOJQYT9M4CWRt3"
        mapping="pathname"
        strict="0"
        reactions-enabled="1"
        emit-metadata="0"
        input-position="top"
        :theme="isDark ? 'transparent_dark' : 'light'"
        lang="zh-CN"
    />
  </div>
</template>

<style>
.comments-container {
  /* max-height: 640px; */
  /* overflow-y: scroll; */
  margin-top: 5rem;
}
</style>