import { Modal, ModalFuncProps } from 'antd';

interface ConfirmDialogProps extends Omit<ModalFuncProps, 'onOk'> {
  title?: string;
  content: string;
  onConfirm: () => Promise<void> | void;
  confirmText?: string;
  cancelText?: string;
}

export const confirmDelete = (
  itemName: string = '此项目',
  onConfirm: () => Promise<void> | void
): void => {
  Modal.confirm({
    title: '确认删除',
    content: `确定要删除${itemName}吗？此操作不可恢复。`,
    okText: '删除',
    okType: 'danger',
    cancelText: '取消',
    onOk: onConfirm,
  });
};

export const confirmAction = (
  title: string,
  content: string,
  onConfirm: () => Promise<void> | void,
  okText: string = '确定',
  okType: 'primary' | 'danger' = 'primary'
): void => {
  Modal.confirm({
    title,
    content,
    okText,
    okType,
    cancelText: '取消',
    onOk: onConfirm,
  });
};

export const showConfirmDialog = (props: ConfirmDialogProps): void => {
  const {
    title = '确认操作',
    content,
    onConfirm,
    confirmText = '确定',
    cancelText = '取消',
    ...rest
  } = props;

  Modal.confirm({
    title,
    content,
    okText: confirmText,
    cancelText,
    onOk: onConfirm,
    ...rest,
  });
};

export default {
  confirmDelete,
  confirmAction,
  showConfirmDialog,
};
