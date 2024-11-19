<template>
  <v-card class="mx-auto my-4 custom-card" max-width="700">
    <v-row>
      <v-col cols="9">
        <v-card-title>{{ title }}</v-card-title>
        <v-card-subtitle>{{ description }}</v-card-subtitle>
      </v-col>
      <v-col cols="3" class="d-flex align-center justify-end">
        <v-img :src="imageLink"></v-img>
      </v-col>
    </v-row>
  </v-card>
</template>

<script>
export default {
  name: 'CustomCard',
  props: {
    title: {
      type: String,
      required: true
    },
    description: {
      type: String,
      required: true
    },
    imageLink: {
      type: String,
      required: true
    },
  }
}
</script>

<style scoped>
.v-card-title {
  font-weight: bold;
  font-size: 1.5em;
}
.v-card-subtitle {
  font-size: 1em;
  color: #666;
  white-space: normal;      
  word-wrap: break-word;
}
.custom-card {
  padding: 40px 40px 40px 20px;
}

</style>