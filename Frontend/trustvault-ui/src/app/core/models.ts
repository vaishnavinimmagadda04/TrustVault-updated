export interface UserSession {
  userId: number;
  token: string;
  name: string;
  email: string;
  role: 'Admin' | 'Owner' | 'Viewer';
}

export interface DocumentItem {
  documentId: number;
  title: string;
  category: string;
  originalFileName: string;
  uploadDate: string;
  ownerId: number;
  ownerName: string;
  accessLevel: 'OWNER' | 'ADMIN' | 'READ' | 'EDIT';
}

export interface DashboardData {
  totalDocuments: number;
  myUploads: number;
  recentDocuments: DocumentItem[];
}

export interface UserOption {
  userId: number;
  name: string;
  email: string;
}

export interface DocumentPermission {
  permissionId: number;
  documentId: number;
  userId: number;
  userName: string;
  email: string;
  permission: 'READ' | 'EDIT';
  grantedBy: number;
  grantedDate: string;
  isActive: boolean;
}

export interface ActivityItem {
  logId: number;
  userId: number | null;
  userName: string;
  documentId: number | null;
  documentTitle: string;
  action: string;
  wasAllowed: boolean;
  timestamp: string;
  ipAddress: string | null;
  details: string | null;
}

export interface AdminActivityData {
  recentUploads: ActivityItem[];
  recentDownloads: ActivityItem[];
  permissionChanges: ActivityItem[];
  failedAccessAttempts: ActivityItem[];
}
