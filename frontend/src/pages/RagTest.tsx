import React, { useEffect, useState, useRef } from 'react';
import { Card, Row, Col, Form, Input, Select, Button, message, Table, Tag, Space, Divider, InputNumber, Tabs, Popconfirm, Modal, Alert, Pagination, Upload, Radio, RadioChangeEvent } from 'antd';
import { DeleteOutlined, CopyOutlined, FileTextOutlined, SplitCellsOutlined, DatabaseOutlined, SearchOutlined, RobotOutlined, UploadOutlined, InboxOutlined } from '@ant-design/icons';
import api from '../services/api';

const { Option } = Select;
const { TextArea } = Input;

interface RagDocument {
  id: string;
  fileName: string;
  fileType?: string;
  fileSize: number;
  splitMethod?: string;
  chunkSize?: number;
  chunkOverlap?: number;
  chunkCount: number;
  status: string;
  errorMessage?: string;
  createdAt: string;
}

interface RagDocumentChunk {
  id: string;
  documentId: string;
  content: string;
  chunkIndex: number;
  vectorId?: string;
  createdAt: string;
}

interface LLMConfig {
  id: string;
  name: string;
  provider: string;
  defaultModel: string;
  isEnabled: boolean;
}

interface VectorDocument {
  id: string;
  content: string;
  score?: number;
  documentId?: string;
  chunkIndex?: number;
}

const RagTest: React.FC = () => {
  const [documents, setDocuments] = useState<RagDocument[]>([]);
  const [splitMethods, setSplitMethods] = useState<any[]>([]);
  const [fileTypes, setFileTypes] = useState<any[]>([]);
  const [llmConfigs, setLLMConfigs] = useState<LLMConfig[]>([]);
  const [loading, setLoading] = useState(false);
  const [testResult, setTestResult] = useState<any>(null);
  const [selectedDocument, setSelectedDocument] = useState<RagDocument | null>(null);
  const [chunks, setChunks] = useState<RagDocumentChunk[]>([]);
  const [chunksModalVisible, setChunksModalVisible] = useState(false);
  const [ragQueryResult, setRagQueryResult] = useState<any>(null);
  const [vectorizing, setVectorizing] = useState(false);
  const [ragLoading, setRagLoading] = useState(false);
  const [form] = Form.useForm();
  const [uploadForm] = Form.useForm();
  const [ragForm] = Form.useForm();
  
  // 文件上传相关状态
  const [uploadMode, setUploadMode] = useState<'text' | 'file'>('file');
  const [fileList, setFileList] = useState<any[]>([]);
  const [uploading, setUploading] = useState(false);
  const [supportedTypes, setSupportedTypes] = useState<any[]>([]);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const initializedRef = useRef(false);

  // 向量文档搜索状态
  const [vectorDocs, setVectorDocs] = useState<VectorDocument[]>([]);
  const [vectorDocsLoading, setVectorDocsLoading] = useState(false);
  const [vectorDocsTotal, setVectorDocsTotal] = useState(0);
  const [vectorDocsPage, setVectorDocsPage] = useState(1);
  const [vectorDocsPageSize, setVectorDocsPageSize] = useState(10);
  const [vectorDocsKeyword, setVectorDocsKeyword] = useState('');
  const [vectorSearchForm] = Form.useForm();

  const loadData = async () => {
    setLoading(true);
    try {
      const [docsRes, methodsRes, typesRes, llmRes, supportedRes] = await Promise.all([
        api.get('/rag/documents'),
        api.get('/rag/split-methods'),
        api.get('/rag/file-types'),
        api.get('/llmconfigs'),
        api.get('/rag/supported-types')
      ]);
      setDocuments(docsRes.data || []);
      setSplitMethods(methodsRes.data || []);
      setFileTypes(typesRes.data || []);
      setLLMConfigs((llmRes.data || []).filter((c: LLMConfig) => c.isEnabled));
      setSupportedTypes(supportedRes.data || []);
    } catch (error) {
      console.error('加载数据失败', error);
      message.error('加载数据失败');
    } finally {
      setLoading(false);
    }
  };

  const loadVectorDocs = async (page = 1, pageSize = 10, keyword = '') => {
    setVectorDocsLoading(true);
    try {
      const params = new URLSearchParams({
        page: page.toString(),
        pageSize: pageSize.toString(),
        keyword: keyword
      });
      const response = await api.get(`/rag/vector-documents?${params}`);
      setVectorDocs(response.data.items || []);
      setVectorDocsTotal(response.data.total || 0);
      setVectorDocsPage(page);
      setVectorDocsPageSize(pageSize);
    } catch (error) {
      console.error('加载向量文档失败', error);
      message.error('加载向量文档失败');
    } finally {
      setVectorDocsLoading(false);
    }
  };

  const handleDeleteVectorDoc = async (id: string) => {
    try {
      await api.delete(`/rag/vector-documents/${id}`);
      message.success('删除成功');
      loadVectorDocs(vectorDocsPage, vectorDocsPageSize, vectorDocsKeyword);
    } catch (error) {
      message.error('删除失败');
    }
  };

  const handleSearchVectorDocs = () => {
    const keyword = vectorSearchForm.getFieldValue('keyword') || '';
    setVectorDocsKeyword(keyword);
    loadVectorDocs(1, vectorDocsPageSize, keyword);
  };

  useEffect(() => {
    if (!initializedRef.current) {
      initializedRef.current = true;
      loadData();
      loadVectorDocs();
      form.setFieldsValue({
        splitMethod: 'recursive',
        chunkSize: 500,
        chunkOverlap: 50,
      });
      uploadForm.setFieldsValue({
        splitMethod: 'recursive',
        chunkSize: 500,
        chunkOverlap: 50,
      });
    }
  }, []);

  const handleTestSplit = async () => {
    try {
      const values = await form.validateFields();
      if (!values.content) {
        message.warning('请输入文本内容');
        return;
      }
      const response = await api.post('/rag/test-split', values);
      setTestResult(response.data);
      message.success('分割测试完成');
    } catch (error) {
      message.error('测试失败');
    }
  };

  const handleUploadDocument = async () => {
    try {
      const values = await uploadForm.validateFields();
      if (!values.content || !values.fileName) {
        message.warning('请输入文件名和内容');
        return;
      }
      await api.post('/rag/documents', values);
      message.success('上传成功');
      loadData();
      uploadForm.resetFields(['content', 'fileName']);
    } catch (error) {
      message.error('上传失败');
    }
  };

  const handleFileUpload = async () => {
    if (fileList.length === 0) {
      message.warning('请选择要上传的文件');
      return;
    }

    const formData = new FormData();
    formData.append('file', fileList[0]);
    
    const splitMethod = uploadForm.getFieldValue('splitMethod');
    const chunkSize = uploadForm.getFieldValue('chunkSize');
    const chunkOverlap = uploadForm.getFieldValue('chunkOverlap');
    
    if (splitMethod) formData.append('splitMethod', splitMethod);
    if (chunkSize) formData.append('chunkSize', chunkSize.toString());
    if (chunkOverlap) formData.append('chunkOverlap', chunkOverlap.toString());

    setUploading(true);
    try {
      await api.post('/rag/documents/upload-file', formData, {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      });
      message.success('文件上传成功');
      setFileList([]);
      loadData();
    } catch (error: any) {
      message.error(error.response?.data?.message || '上传失败');
    } finally {
      setUploading(false);
    }
  };

  const handleFileChange = (info: any) => {
    setFileList(info.fileList.slice(-1));
    
    if (info.fileList.length > 0) {
      const file = info.fileList[0].originFileObj || info.fileList[0];
      const fileName = file.name;
      const ext = fileName.substring(fileName.lastIndexOf('.')).toLowerCase();
      
      uploadForm.setFieldsValue({
        fileType: ext,
      });
    }
  };

  const handleUploadModeChange = (e: RadioChangeEvent) => {
    setUploadMode(e.target.value);
    setFileList([]);
    uploadForm.resetFields(['content', 'fileName', 'fileType']);
  };

  const handleDeleteDocument = async (id: string) => {
    try {
      await api.delete(`/rag/documents/${id}`);
      message.success('删除成功');
      loadData();
    } catch (error) {
      message.error('删除失败');
    }
  };

  const handleViewChunks = async (doc: RagDocument) => {
    try {
      const response = await api.get(`/rag/documents/${doc.id}/chunks`);
      setChunks(response.data || []);
      setSelectedDocument(doc);
      setChunksModalVisible(true);
    } catch (error) {
      message.error('加载分块失败');
    }
  };

  const handleVectorize = async (docId: string) => {
    setVectorizing(true);
    try {
      const response = await api.post(`/rag/documents/${docId}/vectorize`);
      message.success(`向量入库完成，成功 ${response.data.successCount} 个分块`);
      loadData();
      loadVectorDocs(vectorDocsPage, vectorDocsPageSize, vectorDocsKeyword);
    } catch (error: any) {
      message.error(error.response?.data?.message || '向量入库失败');
    } finally {
      setVectorizing(false);
    }
  };

  const handleRagQuery = async () => {
    try {
      const values = await ragForm.validateFields();
      if (!values.query) {
        message.warning('请输入查询问题');
        return;
      }
      setRagLoading(true);
      const response = await api.post('/rag/query', values);
      setRagQueryResult(response.data);
      message.success('RAG检索完成');
    } catch (error: any) {
      message.error(error.response?.data?.message || 'RAG检索失败');
    } finally {
      setRagLoading(false);
    }
  };

  const handleCopy = (text: string) => {
    navigator.clipboard.writeText(text);
    message.success('已复制到剪贴板');
  };

  const statusColors: Record<string, string> = {
    Pending: 'default',
    Processing: 'processing',
    Completed: 'success',
    Failed: 'error',
  };

  const statusLabels: Record<string, string> = {
    Pending: '待处理',
    Processing: '处理中',
    Completed: '已完成',
    Failed: '失败',
  };

  const documentColumns = [
    {
      title: '文件名',
      dataIndex: 'fileName',
      key: 'fileName',
      width: 200,
    },
    {
      title: '类型',
      dataIndex: 'fileType',
      key: 'fileType',
      width: 80,
      render: (type: string) => type || '-',
    },
    {
      title: '分割方式',
      dataIndex: 'splitMethod',
      key: 'splitMethod',
      width: 100,
      render: (method: string) => {
        const m = splitMethods.find((s: any) => s.value === method);
        return m?.label || method || '-';
      },
    },
    {
      title: '分块数',
      dataIndex: 'chunkCount',
      key: 'chunkCount',
      width: 80,
    },
    {
      title: '状态',
      dataIndex: 'status',
      key: 'status',
      width: 100,
      render: (status: string) => (
        <Tag color={statusColors[status]}>{statusLabels[status] || status}</Tag>
      ),
    },
    {
      title: '操作',
      key: 'action',
      width: 280,
      render: (_: any, record: RagDocument) => (
        <Space size="small">
          <Button type="link" size="small" onClick={() => handleViewChunks(record)}>
            查看分块
          </Button>
          <Button 
            type="link" 
            size="small" 
            icon={<DatabaseOutlined />}
            onClick={() => handleVectorize(record.id)}
            loading={vectorizing}
          >
            向量入库
          </Button>
          <Popconfirm
            title="确定要删除这个文档吗？"
            onConfirm={() => handleDeleteDocument(record.id)}
            okText="确定"
            cancelText="取消"
          >
            <Button type="link" size="small" danger icon={<DeleteOutlined />}>
              删除
            </Button>
          </Popconfirm>
        </Space>
      ),
    },
  ];

  const chunkColumns = [
    {
      title: '序号',
      dataIndex: 'chunkIndex',
      key: 'chunkIndex',
      width: 60,
    },
    {
      title: '内容',
      dataIndex: 'content',
      key: 'content',
      ellipsis: true,
      render: (content: string) => (
        <div style={{ maxHeight: 100, overflow: 'auto' }}>
          {content.substring(0, 200)}{content.length > 200 ? '...' : ''}
        </div>
      ),
    },
    {
      title: '向量ID',
      dataIndex: 'vectorId',
      key: 'vectorId',
      width: 120,
      render: (id: string) => id ? <Tag color="green">已入库</Tag> : <Tag>未入库</Tag>,
    },
    {
      title: '操作',
      key: 'action',
      width: 80,
      render: (_: any, record: RagDocumentChunk) => (
        <Button 
          type="link" 
          icon={<CopyOutlined />} 
          onClick={() => handleCopy(record.content)}
        >
          复制
        </Button>
      ),
    },
  ];

  const testResultColumns = [
    {
      title: '序号',
      dataIndex: 'chunkIndex',
      key: 'chunkIndex',
      width: 60,
      render: (_: any, __: any, index: number) => index + 1,
    },
    {
      title: '内容',
      dataIndex: 'content',
      key: 'content',
      ellipsis: true,
    },
    {
      title: '字符数',
      key: 'length',
      width: 80,
      render: (_: any, record: any) => record.content?.length || 0,
    },
    {
      title: '操作',
      key: 'action',
      width: 80,
      render: (_: any, record: any) => (
        <Button 
          type="link" 
          icon={<CopyOutlined />} 
          onClick={() => handleCopy(record.content)}
        >
          复制
        </Button>
      ),
    },
  ];

  const vectorDocColumns = [
    {
      title: 'ID',
      dataIndex: 'id',
      key: 'id',
      width: 280,
      ellipsis: true,
    },
    {
      title: '内容预览',
      dataIndex: 'content',
      key: 'content',
      ellipsis: true,
      render: (content: string) => content?.substring(0, 100) + (content?.length > 100 ? '...' : ''),
    },
    {
      title: '文档ID',
      dataIndex: 'documentId',
      key: 'documentId',
      width: 200,
      ellipsis: true,
    },
    {
      title: '分块序号',
      dataIndex: 'chunkIndex',
      key: 'chunkIndex',
      width: 100,
    },
    {
      title: '操作',
      key: 'action',
      width: 100,
      render: (_: any, record: VectorDocument) => (
        <Space>
          <Button type="link" size="small" icon={<CopyOutlined />} onClick={() => handleCopy(record.content)}>
            复制
          </Button>
          <Popconfirm
            title="确定要删除此向量文档吗？"
            onConfirm={() => handleDeleteVectorDoc(record.id)}
            okText="确定"
            cancelText="取消"
          >
            <Button type="link" size="small" danger icon={<DeleteOutlined />}>
              删除
            </Button>
          </Popconfirm>
        </Space>
      ),
    },
  ];

  return (
    <div>
      <h2>RAG测试</h2>
      
      <Tabs
        defaultActiveKey="test"
        items={[
          {
            key: 'test',
            label: '文本分割测试',
            icon: <SplitCellsOutlined />,
            children: (
              <Row gutter={16}>
                <Col span={12}>
                  <Card title="输入文本" extra={<Button icon={<CopyOutlined />} onClick={() => handleCopy(form.getFieldValue('content') || '')}>复制</Button>}>
                    <Form form={form} layout="vertical">
                      <Form.Item label="分割方式" name="splitMethod">
                        <Select placeholder="选择分割方式">
                          {splitMethods.map((m: any) => (
                            <Option key={m.value} value={m.value}>
                              {m.label} - {m.description}
                            </Option>
                          ))}
                        </Select>
                      </Form.Item>
                      <Row gutter={16}>
                        <Col span={12}>
                          <Form.Item label="分块大小" name="chunkSize">
                            <InputNumber min={100} max={10000} style={{ width: '100%' }} />
                          </Form.Item>
                        </Col>
                        <Col span={12}>
                          <Form.Item label="重叠大小" name="chunkOverlap">
                            <InputNumber min={0} max={1000} style={{ width: '100%' }} />
                          </Form.Item>
                        </Col>
                      </Row>
                      <Form.Item label="文本内容" name="content">
                        <TextArea rows={15} placeholder="请输入或粘贴要分割的文本内容..." />
                      </Form.Item>
                      <Form.Item>
                        <Button type="primary" onClick={handleTestSplit} block>
                          测试分割
                        </Button>
                      </Form.Item>
                    </Form>
                  </Card>
                </Col>
                <Col span={12}>
                  <Card title={`分割结果 ${testResult ? `(${testResult.chunkCount} 个分块)` : ''}`}>
                    {testResult ? (
                      <Table
                        dataSource={testResult.chunks}
                        columns={testResultColumns}
                        rowKey={(_: any, index?: number) => `chunk-${index ?? 0}`}
                        pagination={false}
                        scroll={{ y: 500 }}
                      />
                    ) : (
                      <div style={{ textAlign: 'center', padding: 50, color: '#999' }}>
                        请输入文本并点击"测试分割"按钮
                      </div>
                    )}
                  </Card>
                </Col>
              </Row>
            ),
          },
          {
            key: 'upload',
            label: '文档管理',
            icon: <FileTextOutlined />,
            children: (
              <Row gutter={16}>
                <Col span={8}>
                  <Card title="上传文档">
                    <Form form={uploadForm} layout="vertical">
                      <Form.Item label="上传方式">
                        <Radio.Group value={uploadMode} onChange={handleUploadModeChange}>
                          <Radio.Button value="file">文件上传</Radio.Button>
                          <Radio.Button value="text">文本粘贴</Radio.Button>
                        </Radio.Group>
                      </Form.Item>
                      
                      {uploadMode === 'file' ? (
                        <>
                          <Form.Item label="选择文件">
                            <Upload.Dragger
                              fileList={fileList}
                              beforeUpload={() => false}
                              onChange={handleFileChange}
                              accept={supportedTypes.map((t: any) => t.extension).join(',')}
                              maxCount={1}
                            >
                              <p className="ant-upload-drag-icon">
                                <InboxOutlined />
                              </p>
                              <p className="ant-upload-text">点击或拖拽文件到此区域上传</p>
                              <p className="ant-upload-hint">
                                支持的文件类型: txt, md, py, js, ts, java, cs, go, html, css, json, yaml, csv 等
                              </p>
                            </Upload.Dragger>
                          </Form.Item>
                          {fileList.length > 0 && (
                            <Form.Item label="文件类型">
                              <Tag color="blue">{fileList[0].name?.substring(fileList[0].name.lastIndexOf('.')) || '未知'}</Tag>
                            </Form.Item>
                          )}
                        </>
                      ) : (
                        <>
                          <Form.Item label="文件名" name="fileName" rules={[{ required: uploadMode === 'text' }]}>
                            <Input placeholder="例如: document.txt" />
                          </Form.Item>
                          <Form.Item label="文件类型" name="fileType">
                            <Select placeholder="选择文件类型" allowClear>
                              {fileTypes.map((t: any) => (
                                <Option key={t.ext} value={t.ext}>
                                  {t.name} ({t.ext}) {t.needSplit ? '' : '- 不分割'}
                                </Option>
                              ))}
                            </Select>
                          </Form.Item>
                          <Form.Item label="文件内容" name="content" rules={[{ required: uploadMode === 'text' }]}>
                            <TextArea rows={8} placeholder="请输入或粘贴文件内容..." />
                          </Form.Item>
                        </>
                      )}
                      
                      <Form.Item label="分割方式" name="splitMethod">
                        <Select placeholder="选择分割方式">
                          {splitMethods.map((m: any) => (
                            <Option key={m.value} value={m.value}>
                              {m.label} - {m.description}
                            </Option>
                          ))}
                        </Select>
                      </Form.Item>
                      <Row gutter={16}>
                        <Col span={12}>
                          <Form.Item label="分块大小" name="chunkSize">
                            <InputNumber min={100} max={10000} style={{ width: '100%' }} />
                          </Form.Item>
                        </Col>
                        <Col span={12}>
                          <Form.Item label="重叠大小" name="chunkOverlap">
                            <InputNumber min={0} max={1000} style={{ width: '100%' }} />
                          </Form.Item>
                        </Col>
                      </Row>
                      <Form.Item>
                        <Button 
                          type="primary" 
                          onClick={uploadMode === 'file' ? handleFileUpload : handleUploadDocument} 
                          loading={uploading}
                          block
                        >
                          {uploadMode === 'file' ? '上传文件' : '上传文档'}
                        </Button>
                      </Form.Item>
                    </Form>
                  </Card>
                </Col>
                <Col span={16}>
                  <Card title="已上传文档">
                    <Table
                      dataSource={documents}
                      columns={documentColumns}
                      rowKey="id"
                      loading={loading}
                      pagination={{ pageSize: 10 }}
                      scroll={{ x: 900 }}
                    />
                  </Card>
                </Col>
              </Row>
            ),
          },
          {
            key: 'vector',
            label: '向量文档',
            icon: <DatabaseOutlined />,
            children: (
              <Card 
                title="向量文档库"
                extra={
                  <Space>
                    <span>共 {vectorDocsTotal} 条记录</span>
                  </Space>
                }
              >
                <Form form={vectorSearchForm} layout="inline" style={{ marginBottom: 16 }}>
                  <Form.Item name="keyword">
                    <Input 
                      placeholder="输入关键词搜索..." 
                      style={{ width: 300 }}
                      onPressEnter={handleSearchVectorDocs}
                    />
                  </Form.Item>
                  <Form.Item>
                    <Button type="primary" icon={<SearchOutlined />} onClick={handleSearchVectorDocs}>
                      搜索
                    </Button>
                    <Button style={{ marginLeft: 8 }} onClick={() => {
                      vectorSearchForm.resetFields();
                      setVectorDocsKeyword('');
                      loadVectorDocs(1, vectorDocsPageSize, '');
                    }}>
                      重置
                    </Button>
                  </Form.Item>
                </Form>
                <Table
                  dataSource={vectorDocs}
                  columns={vectorDocColumns}
                  rowKey="id"
                  loading={vectorDocsLoading}
                  pagination={{
                    current: vectorDocsPage,
                    pageSize: vectorDocsPageSize,
                    total: vectorDocsTotal,
                    showSizeChanger: true,
                    showQuickJumper: true,
                    showTotal: (total) => `共 ${total} 条`,
                    onChange: (page, pageSize) => loadVectorDocs(page, pageSize, vectorDocsKeyword),
                  }}
                  scroll={{ x: 1000 }}
                />
              </Card>
            ),
          },
          {
            key: 'rag',
            label: 'RAG检索',
            icon: <SearchOutlined />,
            children: (
              <Row gutter={16}>
                <Col span={10}>
                  <Card title="检索配置">
                    <Alert 
                      message="请确保已在RAG配置中配置向量数据库和向量化接口" 
                      type="info" 
                      showIcon 
                      style={{ marginBottom: 16 }}
                    />
                    <Form form={ragForm} layout="vertical">
                      <Form.Item label="选择大模型" name="llmConfigId" rules={[{ required: true }]}>
                        <Select placeholder="选择用于回答的大模型配置">
                          {llmConfigs.map(c => (
                            <Option key={c.id} value={c.id}>
                              {c.name} ({c.provider} - {c.defaultModel})
                            </Option>
                          ))}
                        </Select>
                      </Form.Item>
                      <Form.Item label="检索数量" name="topK" initialValue={5}>
                        <InputNumber min={1} max={20} style={{ width: '100%' }} />
                      </Form.Item>
                      <Form.Item label="相似度阈值" name="scoreThreshold" initialValue={0.5}>
                        <InputNumber min={0} max={1} step={0.1} style={{ width: '100%' }} />
                      </Form.Item>
                      <Form.Item label="系统提示词" name="systemPrompt">
                        <TextArea 
                          rows={3} 
                          placeholder="可选，自定义系统提示词..."
                          defaultValue="你是一个智能助手，请根据提供的上下文信息回答用户问题。如果上下文中没有相关信息，请诚实地说不知道。"
                        />
                      </Form.Item>
                      <Form.Item label="查询问题" name="query" rules={[{ required: true }]}>
                        <TextArea rows={4} placeholder="请输入要查询的问题..." />
                      </Form.Item>
                      <Form.Item>
                        <Button 
                          type="primary" 
                          onClick={handleRagQuery} 
                          block 
                          loading={ragLoading}
                          icon={<RobotOutlined />}
                        >
                          RAG检索
                        </Button>
                      </Form.Item>
                    </Form>
                  </Card>
                </Col>
                <Col span={14}>
                  <Card title="检索结果">
                    {ragQueryResult ? (
                      <div>
                        <Divider orientation="left">AI回答</Divider>
                        <div style={{ 
                          padding: 16, 
                          background: '#f5f5f5', 
                          borderRadius: 8,
                          whiteSpace: 'pre-wrap',
                          minHeight: 100 
                        }}>
                          {ragQueryResult.answer}
                        </div>
                        
                        <Divider orientation="left">相关文档片段 ({ragQueryResult.sources?.length || 0})</Divider>
                        {ragQueryResult.sources?.map((source: any, index: number) => (
                          <Card 
                            key={index} 
                            size="small" 
                            style={{ marginBottom: 8 }}
                            title={
                              <Space>
                                <Tag color="blue">#{index + 1}</Tag>
                                <span>相似度: {(source.score * 100).toFixed(1)}%</span>
                              </Space>
                            }
                            extra={<Button type="link" icon={<CopyOutlined />} onClick={() => handleCopy(source.content)}>复制</Button>}
                          >
                            <div style={{ maxHeight: 100, overflow: 'auto' }}>
                              {source.content}
                            </div>
                          </Card>
                        ))}
                      </div>
                    ) : (
                      <div style={{ textAlign: 'center', padding: 50, color: '#999' }}>
                        请配置检索参数并输入查询问题
                      </div>
                    )}
                  </Card>
                </Col>
              </Row>
            ),
          },
        ]}
      />

      <Modal
        title={`文档分块 - ${selectedDocument?.fileName}`}
        open={chunksModalVisible}
        onCancel={() => setChunksModalVisible(false)}
        footer={null}
        width={900}
      >
        <Table
          dataSource={chunks}
          columns={chunkColumns}
          rowKey="id"
          pagination={{ pageSize: 10 }}
          scroll={{ y: 400 }}
        />
      </Modal>
    </div>
  );
};

export default RagTest;
