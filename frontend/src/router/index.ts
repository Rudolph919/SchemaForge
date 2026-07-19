import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      name: 'dashboard',
      component: () => import('@/shared/views/DashboardView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/login',
      name: 'login',
      component: () => import('@/modules/auth/views/LoginView.vue'),
      meta: { requiresAuth: false },
    },
    {
      path: '/register',
      name: 'register',
      component: () => import('@/modules/auth/views/RegisterView.vue'),
      meta: { requiresAuth: false },
    },
  ],
})

router.beforeEach((to) => {
  const authStore = useAuthStore()

  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    return { name: 'login' }
  }

  if (!to.meta.requiresAuth && authStore.isAuthenticated && (to.name === 'login' || to.name === 'register')) {
    return { name: 'dashboard' }
  }

  return true
})

export default router
