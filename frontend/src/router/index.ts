import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      component: () => import('@/shared/layout/AppShell.vue'),
      meta: { requiresAuth: true },
      children: [
        { path: '', redirect: '/projects' },
        {
          path: 'projects',
          name: 'projects',
          component: () => import('@/modules/workspaces/views/ProjectsListView.vue'),
        },
        {
          path: 'projects/:id',
          name: 'project-detail',
          component: () => import('@/modules/workspaces/views/ProjectDetailView.vue'),
        },
        {
          path: 'teams',
          name: 'teams',
          component: () => import('@/modules/organizations/views/TeamsListView.vue'),
        },
        {
          path: 'teams/:id',
          name: 'team-detail',
          component: () => import('@/modules/organizations/views/TeamDetailView.vue'),
        },
        {
          path: 'members',
          name: 'members',
          component: () => import('@/modules/organizations/views/MembersView.vue'),
        },
        {
          path: 'settings',
          name: 'settings',
          component: () => import('@/modules/organizations/views/SettingsView.vue'),
        },
      ],
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
    return { name: 'projects' }
  }

  return true
})

export default router
